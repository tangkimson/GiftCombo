using Dapper;
using GiftCombo.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Transactions;

namespace GiftCombo.Database.Repositories
{
    public class SaleRepository
    {
        // Replace the CreateSale method completely:
        public int CreateSale(DateTime? date, IEnumerable<SaleLineInput> lines)
        {
            System.Diagnostics.Debug.WriteLine("NUCLEAR RAW SQL ONLY");

            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleDb"].ConnectionString;

            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);

            try
            {
                connection.Open();
                System.Diagnostics.Debug.WriteLine("Connection opened");

                // 1. Insert sale with raw SQL
                using var saleCmd = connection.CreateCommand();
                saleCmd.CommandText = "INSERT INTO sale (sale_date) VALUES (SYSDATE)";
                saleCmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Sale inserted");

                // 2. Get sale ID with raw SQL
                using var idCmd = connection.CreateCommand();
                idCmd.CommandText = "SELECT MAX(sale_id) FROM sale";
                var saleId = Convert.ToInt32(idCmd.ExecuteScalar());
                System.Diagnostics.Debug.WriteLine($"Sale ID: {saleId}");

                // 3. Get prices and build completely raw SQL
                foreach (var l in lines)
                {
                    if (l.ItemId.HasValue)
                    {
                        // Get price with raw SQL
                        using var priceCmd = connection.CreateCommand();
                        priceCmd.CommandText = $"SELECT price FROM item WHERE item_id = {l.ItemId.Value}";
                        var price = Convert.ToDecimal(priceCmd.ExecuteScalar());
                        var lineTotal = price * l.Quantity;

                        System.Diagnostics.Debug.WriteLine($"Price: {price}, Total: {lineTotal}");

                        // Insert with COMPLETELY raw SQL - NO PARAMETERS AT ALL
                        var rawSql = $"INSERT INTO sale_line (sale_id, item_id, combo_id, quantity, line_total) VALUES ({saleId}, {l.ItemId.Value}, NULL, {l.Quantity}, {lineTotal})";

                        System.Diagnostics.Debug.WriteLine($"Raw SQL: {rawSql}");

                        using var lineCmd = connection.CreateCommand();
                        lineCmd.CommandText = rawSql;

                        System.Diagnostics.Debug.WriteLine("About to execute raw SQL");
                        lineCmd.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("Raw SQL executed successfully");
                    }
                }

                // Commit with raw SQL
                using var commitCmd = connection.CreateCommand();
                commitCmd.CommandText = "COMMIT";
                commitCmd.ExecuteNonQuery();

                System.Diagnostics.Debug.WriteLine($"SUCCESS: {saleId}");
                return saleId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
                throw;
            }
        }

        public IEnumerable<Sale> GetRecent(int takeLast = 50)
        {
            using var db = OracleContext.GetOpenConnection();
            return db.Query<Sale>(
              @"SELECT s.sale_id    AS SaleId,
               s.sale_date  AS SaleDate,
               NVL(SUM(sl.line_total), 0) AS Total
        FROM   sale s
        LEFT JOIN sale_line sl ON sl.sale_id = s.sale_id
        GROUP BY s.sale_id, s.sale_date
        ORDER  BY s.sale_date DESC
        FETCH  FIRST :n ROWS ONLY",
              new { n = takeLast });
        }
    }
}
