using Microsoft.Data.Sqlite;

namespace OnigiriShop.Data.Interfaces
{
    public interface ISqliteConnectionFactory
    {
        SqliteConnection CreateConnection();
    }
}
