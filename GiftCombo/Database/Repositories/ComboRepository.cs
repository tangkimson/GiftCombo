using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using GiftCombo.Models;

namespace GiftCombo.Database.Repositories
{
    public class ComboRepository
    {
        private const string SelectBase = @"
            SELECT  combo_id AS ComboId,
                    name     AS Name,
                    price    AS Price
            FROM    combo";

        public IEnumerable<Combo> GetAll()
        {
            System.Diagnostics.Debug.WriteLine("ComboRepository.GetAll: Starting");

            using var db = OracleContext.GetOpenConnection();

            try
            {
                var combos = db.Query<Combo>(SelectBase + " ORDER BY name").ToList();
                System.Diagnostics.Debug.WriteLine($"ComboRepository.GetAll: Found {combos.Count} combos");

                foreach (var combo in combos)
                {
                    System.Diagnostics.Debug.WriteLine($"  Combo: {combo.ComboId} - {combo.Name}");
                }

                return combos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ComboRepository.GetAll: Error - {ex.Message}");
                throw;
            }
        }

        public class ComboItemDto
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
            public string ItemName { get; set; } = "";
        }

        public ComboWithItems? GetWithItems(int comboId)
        {
            using var db = OracleContext.GetOpenConnection();

            System.Diagnostics.Debug.WriteLine($"GetWithItems: Looking for combo {comboId}");

            // First, get the combo header
            const string comboSql = @"
        SELECT combo_id AS ComboId, name AS Name, price AS Price 
        FROM combo 
        WHERE combo_id = :comboId";

            var combo = db.QuerySingleOrDefault<Combo>(comboSql, new { comboId });

            if (combo == null)
            {
                System.Diagnostics.Debug.WriteLine($"GetWithItems: Combo {comboId} not found in database");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"GetWithItems: Found combo {combo.ComboId} - '{combo.Name}' (price: {combo.Price})");

            // Create result with combo data
            var result = new ComboWithItems
            {
                ComboId = combo.ComboId,
                Name = combo.Name,
                Price = combo.Price
            };

            // Then get the combo items using strongly-typed DTO
            const string itemsSql = @"
        SELECT ci.item_id AS ItemId, 
               ci.quantity AS Quantity, 
               i.name AS ItemName
        FROM combo_item ci
        JOIN item i ON i.item_id = ci.item_id
        WHERE ci.combo_id = :comboId
        ORDER BY i.name";

            var items = db.Query<ComboItemDto>(itemsSql, new { comboId }).ToList();

            System.Diagnostics.Debug.WriteLine($"GetWithItems: Found {items.Count} items for combo {comboId}");

            foreach (var item in items)
            {
                if (item.ItemId > 0 && item.Quantity > 0)
                {
                    var itemLine = new ComboItemLine(item.ItemId, item.Quantity, item.ItemName ?? "");
                    result.Items.Add(itemLine);
                    System.Diagnostics.Debug.WriteLine($"GetWithItems: Added item {item.ItemId} - '{item.ItemName}' (qty: {item.Quantity})");
                }
            }

            System.Diagnostics.Debug.WriteLine($"GetWithItems: Returning combo {result.ComboId} with {result.Items.Count} items");
            return result;
        }
        /// <summary>Add combo header + combo_item lines in one ACID scope.</summary>
        public int Insert(ComboWithItems combo)
        {
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted });

            using var db = OracleContext.GetOpenConnection();

            // 1. Insert header using Oracle's RETURNING clause
            const string sqlCombo = @"
        INSERT INTO combo (name, price)
        VALUES (:Name, :Price)
        RETURNING combo_id INTO :NewId";

            var parameters = new DynamicParameters();
            parameters.Add("Name", combo.Name);
            parameters.Add("Price", combo.Price);
            parameters.Add("NewId", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            db.Execute(sqlCombo, parameters);

            // Get the returned ID (Oracle returns as decimal, convert to int)
            int comboId = Convert.ToInt32(parameters.Get<decimal>("NewId"));

            System.Diagnostics.Debug.WriteLine($"Inserted combo with ID: {comboId}");

            // 2. Insert mapping lines (only if there are items)
            if (combo.Items.Any())
            {
                const string sqlItem = @"
            INSERT INTO combo_item(combo_id, item_id, quantity)
            VALUES (:comboId, :itemId, :qty)";

                foreach (var ln in combo.Items.Where(l => l.ItemId > 0 && l.Quantity > 0))
                {
                    db.Execute(sqlItem,
                               new { comboId, itemId = ln.ItemId, qty = ln.Quantity });
                }
            }
            scope.Complete();
            return comboId;
        }

        public void Delete(int comboId)
        {
            using var db = OracleContext.GetOpenConnection();
            db.Execute("DELETE FROM combo WHERE combo_id = :comboId", new { comboId });
            // ON DELETE CASCADE removes combo_item rows automatically
        }

        public void UpdateItems(int comboId, IEnumerable<ComboItemLine> lines)
        {
            using var db = OracleContext.GetOpenConnection();
            using var tx = db.BeginTransaction();

            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateItems: Starting update for combo {comboId}");

                // First verify the combo exists
                var comboExists = db.QuerySingleOrDefault<int>(
                    "SELECT COUNT(*) FROM combo WHERE combo_id = :id",
                    new { id = comboId }, tx);

                System.Diagnostics.Debug.WriteLine($"UpdateItems: Combo exists check returned {comboExists}");

                if (comboExists == 0)
                {
                    throw new InvalidOperationException($"Combo with ID {comboId} does not exist");
                }

                // Delete existing items
                var deletedRows = db.Execute("DELETE FROM combo_item WHERE combo_id = :id",
                                           new { id = comboId }, tx);

                System.Diagnostics.Debug.WriteLine($"UpdateItems: Deleted {deletedRows} existing items");

                // Filter and validate items before inserting
                var validLines = lines.Where(l => l != null && l.ItemId > 0 && l.Quantity > 0).ToList();

                System.Diagnostics.Debug.WriteLine($"UpdateItems: Found {validLines.Count} valid items to insert");

                // Insert new items
                const string sql = @"INSERT INTO combo_item(combo_id,item_id,quantity)
                           VALUES (:cid,:iid,:qty)";

                foreach (var ln in validLines)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateItems: Inserting item {ln.ItemId} (qty: {ln.Quantity}) for combo {comboId}");

                    db.Execute(sql, new
                    {
                        cid = comboId,
                        iid = ln.ItemId,
                        qty = ln.Quantity
                    }, tx);
                }

                tx.Commit();
                System.Diagnostics.Debug.WriteLine($"UpdateItems: Successfully updated combo {comboId} with {validLines.Count} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateItems: Error - {ex.Message}");
                tx.Rollback();
                throw;
            }
        }
        public void UpdateCombo(int comboId, string name, decimal price)
        {
            const string sql = @"
        UPDATE combo 
        SET name = :name, price = :price 
        WHERE combo_id = :comboId";

            using var db = OracleContext.GetOpenConnection();

            var rowsAffected = db.Execute(sql, new { comboId, name, price });

            System.Diagnostics.Debug.WriteLine($"UpdateCombo: Updated {rowsAffected} combo(s) with ID {comboId}");

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Combo with ID {comboId} not found for update");
            }
        }
    }
}
