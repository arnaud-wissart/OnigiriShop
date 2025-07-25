﻿using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OnigiriShop.Infrastructure;

namespace Tests.Unit;

public class DatabaseBackupBackgroundServiceTests : IDisposable
{
    private readonly string _dbPath = Path.GetTempFileName();
    private readonly SqliteConnection _conn;

    public DatabaseBackupBackgroundServiceTests()
    {
        _conn = new SqliteConnection($"Data Source={_dbPath}");
        _conn.Open();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE T (Id INTEGER PRIMARY KEY, Name TEXT); INSERT INTO T(Name) VALUES('x');";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void HandleChangeAsync_Cree_Un_Fichier_Bak()
    {
        var service = new DatabaseBackupBackgroundService(new NullLogger<DatabaseBackupBackgroundService>());
        service.HandleChangeAsync(_dbPath);
        var bak = _dbPath + ".bak";
        Assert.True(File.Exists(bak));
        using var destConn = new SqliteConnection($"Data Source={bak};Mode=ReadOnly");
        destConn.Open();
        using var cmd = destConn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM T";
        var c = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(1, c);
    }

    public void Dispose()
    {
        _conn.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        if (File.Exists(_dbPath + ".bak")) File.Delete(_dbPath + ".bak");
    }
}