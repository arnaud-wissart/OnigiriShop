using Microsoft.AspNetCore.Hosting;
using Moq;
using OnigiriShop.Services;
using FluentMigrator.Runner;

namespace Tests.Unit;

public class MaintenanceServiceTests
{
    [Fact]
    public async Task RestoreLastBackupAsync_Replaces_Database_With_Backup()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "db.db");
        var backupPath = Path.ChangeExtension(dbPath, ".bak");
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Pooling=False"))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE t(id INTEGER); INSERT INTO t VALUES (1);";
            cmd.ExecuteNonQuery();
        }
        File.Copy(dbPath, backupPath);
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupPath};Pooling=False"))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO t VALUES (2);";
            cmd.ExecuteNonQuery();
        }
        var sqlDir = Path.Combine(tempDir, "SQL");
        Directory.CreateDirectory(sqlDir);
        var schemaPath = Path.Combine(sqlDir, "init_db.sql");
        await File.WriteAllTextAsync(schemaPath, string.Empty);

        var expectedHash = OnigiriShop.Infrastructure.DatabaseInitializer
            .ComputeSchemaHash(schemaPath);
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={backupPath};Pooling=False"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA user_version = {(int)expectedHash};";
            cmd.ExecuteNonQuery();
        }
        Environment.SetEnvironmentVariable("ONIGIRISHOP_DB_PATH", dbPath);

        try
        {
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(tempDir);
            var runner = new Mock<IMigrationRunner>();
            var service = new MaintenanceService(envMock.Object, runner.Object);

            await service.RestoreLastBackupAsync();

            using var connCheck = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Pooling=False"); connCheck.Open();
            var cmdCheck = connCheck.CreateCommand();
            cmdCheck.CommandText = "SELECT COUNT(*) FROM t";
            var count = (long)cmdCheck.ExecuteScalar()!;
            Assert.Equal(2, count);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ONIGIRISHOP_DB_PATH", null);
            Directory.Delete(tempDir, true);
        }
    }
}
