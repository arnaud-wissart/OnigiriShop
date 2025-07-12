using Microsoft.Data.Sqlite;
using OnigiriShop.Data.Interfaces;

namespace OnigiriShop.Data
{
    public class SqliteConnectionFactory(string connectionString) : ISqliteConnectionFactory
    {
        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(connectionString);
        }
    }
}
