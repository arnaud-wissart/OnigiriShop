using Microsoft.Data.Sqlite;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Service en arrière-plan chargé de créer une copie .bak lorsque la base change.
/// </summary>
public class DatabaseBackupBackgroundService(ILogger<DatabaseBackupBackgroundService> logger) : BackgroundService
{
    private readonly ILogger<DatabaseBackupBackgroundService> _logger = logger;
    private FileSystemWatcher? _watcher;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dbPath = DatabasePaths.GetPath();
        var dir = Path.GetDirectoryName(dbPath) ?? ".";
        _watcher = new FileSystemWatcher(dir, Path.GetFileName(dbPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += (_, _) => HandleChangeAsync(dbPath);
        _watcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }

    public void HandleChangeAsync(string dbPath)
    {
        var bakPath = dbPath + ".bak";
        try
        {
            using var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
            using var dest = new SqliteConnection($"Data Source={bakPath};Pooling=False");
            source.Open();
            dest.Open();
            source.BackupDatabase(dest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde de la base de données");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _watcher?.Dispose();
    }
}
