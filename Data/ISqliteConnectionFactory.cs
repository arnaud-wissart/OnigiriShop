using Microsoft.Data.Sqlite;

namespace OnigiriShop.Data
{
    public interface ISqliteConnectionFactory
    {
        SqliteConnection CreateConnection();
    }
}
