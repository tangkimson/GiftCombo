using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace GiftCombo.Database
{
    /// <summary>
    ///  Thin wrapper that hands out OPEN Oracle connections.
    ///  Dispose it (or wrap in using) as soon as you’re done!
    /// </summary>
    public static class OracleContext
    {
        private static readonly string _cs =
            System.Configuration.ConfigurationManager
                   .ConnectionStrings["OracleDb"]
                   .ConnectionString;

        /// <remarks>
        ///  Pooling is on by default in ODP.NET Core, so this is cheap.
        /// </remarks>
        public static IDbConnection GetOpenConnection()
        {
            var conn = new OracleConnection(_cs);
            conn.Open();
            return conn;
        }
    }
}
