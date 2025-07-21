using Microsoft.Data.Sqlite;
using OnigiriShop.Data.Interfaces;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Tests.Unit
{
    // Wraps an existing connection but ignores Dispose so that unit tests can
    // pass the same in-memory connection without it being closed by the service
    internal class NonDisposableConnection(SqliteConnection inner) : DbConnection
    {
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => inner.BeginTransaction(isolationLevel);
        public override void Close() => inner.Close();
        public override void ChangeDatabase(string databaseName) => inner.ChangeDatabase(databaseName);
        public override void Open() => inner.Open();
        [AllowNull]
        public override string ConnectionString { get => inner.ConnectionString; set => inner.ConnectionString = value; }
        public override string Database => inner.Database;
        public override ConnectionState State => inner.State;
        public override string DataSource => inner.DataSource;
        public override string ServerVersion => inner.ServerVersion;
        protected override DbCommand CreateDbCommand() => inner.CreateCommand();
        protected override void Dispose(bool disposing) { /* no-op */ }
    }

    public class FakeSqliteConnectionFactory(SqliteConnection conn) : ISqliteConnectionFactory
    {
        public IDbConnection CreateConnection() => new NonDisposableConnection(conn);
    }
}
