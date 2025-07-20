using Microsoft.Data.Sqlite;
using OnigiriShop.Data.Interfaces;
using System.Data;

namespace Tests.Unit
{
    // Wraps an existing connection but ignores Dispose so that unit tests can
    // pass the same in-memory connection without it being closed by the service
    internal class NonDisposableConnection(SqliteConnection inner) : IDbConnection
    {
        public string ConnectionString { get => inner.ConnectionString; set => inner.ConnectionString = value; }
        public int ConnectionTimeout => inner.DefaultTimeout;
        public string Database => inner.Database;
        public ConnectionState State => inner.State;
        public IDbTransaction BeginTransaction() => inner.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => inner.BeginTransaction(il);
        public void ChangeDatabase(string databaseName) => inner.ChangeDatabase(databaseName);
        public void Close() => inner.Close();
        public IDbCommand CreateCommand() => inner.CreateCommand();
        public void Open() => inner.Open();
        public void Dispose() { /* no-op */ }
    }

    public class FakeSqliteConnectionFactory(SqliteConnection conn) : ISqliteConnectionFactory
    {
        public IDbConnection CreateConnection() => new NonDisposableConnection(conn);
    }
}
