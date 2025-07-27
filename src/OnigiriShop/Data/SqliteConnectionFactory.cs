using Microsoft.Data.Sqlite;
using OnigiriShop.Data.Interfaces;
using System.Data;

namespace OnigiriShop.Data
{
    public class SqliteConnectionFactory(string connectionString, ILogger<SqliteConnectionFactory> logger) : ISqliteConnectionFactory
    {
        public IDbConnection CreateConnection()
        {
            logger.LogDebug("Creating SQLite connection to {ConnectionString}", connectionString);
            return new SqliteConnection(connectionString);
        }
    }
}
