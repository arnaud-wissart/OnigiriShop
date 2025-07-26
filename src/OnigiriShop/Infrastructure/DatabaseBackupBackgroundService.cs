using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Service en arrière-plan chargé de créer une copie .bak lorsque la base change.
/// </summary>
public class DatabaseBackupBackgroundService(
    ILogger<DatabaseBackupBackgroundService> logger,
    HttpDatabaseBackupService backupService,
    IOptions<BackupConfig> options) : BackgroundService
{
    private readonly ILogger<DatabaseBackupBackgroundService> _logger = logger;
    private readonly HttpDatabaseBackupService _backupService = backupService;
    private readonly BackupConfig _config = options.Value;
    private FileSystemWatcher? _watcher;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dbPath = DatabasePaths.GetPath();
        var dir = Path.GetDirectoryName(dbPath) ?? ".";
        _watcher = new FileSystemWatcher(dir, Path.GetFileName(dbPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += async (_, _) => await HandleChangeAsync(dbPath);
        _watcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }

    public async Task HandleChangeAsync(string dbPath)
    {
        var bakPath = dbPath + ".bak";
        try
        {
            using var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
            using var dest = new SqliteConnection($"Data Source={bakPath};Pooling=False");
            source.Open();
            dest.Open();
            source.BackupDatabase(dest);

            if (!string.IsNullOrWhiteSpace(_config.Endpoint))
            {
                try
                {
                    await _backupService.BackupAsync(_config.Endpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la sauvegarde distante");
                }
            }
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
