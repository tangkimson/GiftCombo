using Dapper;
using System.Collections.Generic;
using System.Data;
using GiftCombo.Models;

namespace GiftCombo.Database.Repositories
{
    public class ItemRepository
    {
        // ---------- queries --------------------------------------------------

        private const string SelectBase = @"
SELECT  item_id   AS ItemId,
        name      AS Name,
        item_type AS ItemType,
        price     AS Price,
        stock_qty AS StockQty
FROM    item";

        // ---------- CRUD -----------------------------------------------------

        public IEnumerable<Item> GetAll()
        {
            System.Diagnostics.Debug.WriteLine("ItemRepository.GetAll: Starting");

            using var db = OracleContext.GetOpenConnection();

            try
            {
                var items = db.Query<Item>(SelectBase + " ORDER BY name").ToList();
                System.Diagnostics.Debug.WriteLine($"ItemRepository.GetAll: Found {items.Count} items");

                foreach (var item in items)
                {
                    System.Diagnostics.Debug.WriteLine($"  Item: {item.ItemId} - {item.Name}");
                }

                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ItemRepository.GetAll: Error - {ex.Message}");
                throw;
            }
        }


        public Item? Get(int id)
        {
            using var db = OracleContext.GetOpenConnection();
            return db.QuerySingleOrDefault<Item>(
                       SelectBase + " WHERE item_id = :id",
                       new { id });
        }

        public int Insert(Item item)
        {
            const string sql = @"
INSERT INTO item (name, item_type, price, stock_qty)
VALUES (:Name, :ItemType, :Price, :StockQty)
RETURNING item_id INTO :NewId";

            using var db = OracleContext.GetOpenConnection();

            var parameters = new DynamicParameters();
            parameters.Add("Name", item.Name);
            parameters.Add("ItemType", item.ItemType.ToString());
            parameters.Add("Price", item.Price);
            parameters.Add("StockQty", item.StockQty);
            parameters.Add("NewId", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            db.Execute(sql, parameters);

            return Convert.ToInt32(parameters.Get<decimal>("NewId"));
        }

        public void Update(Item item)
        {
            const string sql = @"
UPDATE item SET
      name      = :Name,
      item_type = :ItemType,
      price     = :Price,
      stock_qty = :StockQty
WHERE item_id   = :ItemId";

            using var db = OracleContext.GetOpenConnection();
            db.Execute(sql, new
            {
                item.ItemId,
                item.Name,
                ItemType = item.ItemType.ToString(),   // ← same trick
                item.Price,
                item.StockQty
            });
        }

        public void Delete(int id)
        {
            using var db = OracleContext.GetOpenConnection();
            db.Execute("DELETE FROM item WHERE item_id = :id", new { id });
        }

        // ---------- stock helper (wraps PL/SQL proc) -------------------------

        public void AddStock(int itemId, int delta /*positive only*/)
        {
            using var db = OracleContext.GetOpenConnection();
            db.Execute("BEGIN add_stock(:itemId, :delta); END;",
                       new { itemId, delta });
        }
    }
}
