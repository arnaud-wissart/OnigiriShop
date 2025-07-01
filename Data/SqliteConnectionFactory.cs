using Microsoft.Data.Sqlite;

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
