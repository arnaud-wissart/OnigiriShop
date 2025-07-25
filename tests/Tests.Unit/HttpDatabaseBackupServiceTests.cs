﻿using Microsoft.Data.Sqlite;
using OnigiriShop.Infrastructure;

namespace Tests.Unit;

public class HttpDatabaseBackupServiceTests : IDisposable
{
    private readonly string _dbPath = Path.GetTempFileName();
    private readonly SqliteConnection _conn;

    public HttpDatabaseBackupServiceTests()
    {
        _conn = new SqliteConnection($"Data Source={_dbPath}");
        _conn.Open();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE Test (Id INTEGER PRIMARY KEY, Name TEXT); INSERT INTO Test(Name) VALUES('A');";
        cmd.ExecuteNonQuery();

        // Copie la base temporaire vers l'emplacement attendu par le service
        var servicePath = DatabasePaths.GetPath();
        var dir = Path.GetDirectoryName(servicePath)!;
        Directory.CreateDirectory(dir);
        File.Copy(_dbPath, servicePath, true);
    }

    [Fact]
    public async Task BackupAsync_CreeUneCopie_MemeSiBaseOuverte()
    {
        var service = new HttpDatabaseBackupService(new HttpClient());
        var dest = _dbPath + ".bak";
        await service.BackupAsync(dest);
        Assert.True(File.Exists(dest));
        using var destConn = new SqliteConnection($"Data Source={dest};Mode=ReadOnly");
        destConn.Open();
        using var cmd = destConn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Test";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(1, count);
    }

    public void Dispose()
    {
        _conn.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        if (File.Exists(_dbPath + ".bak")) File.Delete(_dbPath + ".bak");
        var servicePath = DatabasePaths.GetPath();
        if (File.Exists(servicePath)) File.Delete(servicePath);
    }
}
