using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using OnigiriShop.Infrastructure;

namespace Tests.Unit;

public class DatabaseBackupBackgroundServiceTests : IDisposable
{
    private readonly string _dbPath = Path.GetTempFileName();
    private readonly SqliteConnection _conn;

    public DatabaseBackupBackgroundServiceTests()
    {
        _conn = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        _conn.Open();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE T (Id INTEGER PRIMARY KEY, Name TEXT);"
            + "CREATE TABLE Setting (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);"
            + "INSERT INTO T(Name) VALUES('x');";
        cmd.ExecuteNonQuery();

        // Aligne le chemin attendu par les services sur la base temporaire
        var servicePath = DatabasePaths.GetPath();
        var dir = Path.GetDirectoryName(servicePath)!;
        Directory.CreateDirectory(dir);
        File.Copy(_dbPath, servicePath, true);
    }

    [Fact]
    public async Task HandleChangeAsync_Cree_Un_Fichier_Bak()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new BackupConfig());
        var driveOptions = Microsoft.Extensions.Options.Options.Create(new GitHubBackupConfig());
        var githubSvc = new FakeGitHubBackupService();
        var service = new DatabaseBackupBackgroundService(
            new NullLogger<DatabaseBackupBackgroundService>(),
            new HttpDatabaseBackupService(new HttpClient()),
            githubSvc,
            options,
            driveOptions);
        await service.HandleChangeAsync(_dbPath);
        var bak = _dbPath + ".bak";
        Assert.True(File.Exists(bak));
        using var destConn = new SqliteConnection($"Data Source={bak};Mode=ReadOnly;Pooling=False");
        destConn.Open();
        using var cmd = destConn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM T";
        var c = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.Equal(1, c);
    }

    [Fact]
    public async Task HandleChangeAsync_Sauvegarde_Distante_Si_Config()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new OnigiriShop.Infrastructure.BackupConfig
        {
            Endpoint = _dbPath + ".remote"
        });
        var driveOptions = Microsoft.Extensions.Options.Options.Create(new OnigiriShop.Infrastructure.GitHubBackupConfig
        {
            Token = "x"
        });
        var githubSvc = new FakeGitHubBackupService();
        var service = new DatabaseBackupBackgroundService(
            new NullLogger<DatabaseBackupBackgroundService>(),
            new HttpDatabaseBackupService(new HttpClient()),
            githubSvc,
            options,
            driveOptions);
        await service.HandleChangeAsync(_dbPath);
        Assert.True(File.Exists(options.Value.Endpoint));
        Assert.True(githubSvc.Uploaded);
    }

    public void Dispose()
    {
        _conn.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        if (File.Exists(_dbPath + ".bak")) File.Delete(_dbPath + ".bak");
        if (File.Exists(_dbPath + ".remote")) File.Delete(_dbPath + ".remote");
    }
}
