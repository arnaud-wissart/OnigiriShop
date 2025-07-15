using Microsoft.Data.Sqlite;
using OnigiriShop.Data.Interfaces;
using System.Data;

namespace OnigiriShop.Data
{
    public class SqliteConnectionFactory(string connectionString) : ISqliteConnectionFactory
    {
        public IDbConnection CreateConnection()
        {
            return new SqliteConnection(connectionString);
        }
    }
}
