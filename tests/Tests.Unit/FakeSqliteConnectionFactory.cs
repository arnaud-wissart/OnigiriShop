using Microsoft.Data.Sqlite;
using OnigiriShop.Data.Interfaces;
using System.Data;

namespace Tests.Unit
{
    public class FakeSqliteConnectionFactory(SqliteConnection conn) : ISqliteConnectionFactory
    {
        public IDbConnection CreateConnection() => conn;
    }
}
