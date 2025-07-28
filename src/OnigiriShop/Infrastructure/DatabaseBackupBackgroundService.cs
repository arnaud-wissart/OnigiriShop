using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Service en arrière-plan chargé de créer une copie .bak lorsque la base change.
/// </summary>
public class DatabaseBackupBackgroundService(
    ILogger<DatabaseBackupBackgroundService> logger,
    HttpDatabaseBackupService backupService,
    IGitHubBackupService gitHub,
    IOptions<BackupConfig> options,
    IOptions<GitHubBackupConfig> driveOptions) : BackgroundService
{
    private readonly BackupConfig _config = options.Value;
    private readonly GitHubBackupConfig _driveConfig = driveOptions.Value;

    private FileSystemWatcher? _watcher;
    private FileSystemWatcher? _walWatcher;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dbPath = DatabasePaths.GetPath();
        var dir = Path.GetDirectoryName(dbPath) ?? ".";
        _watcher = new FileSystemWatcher(dir, Path.GetFileName(dbPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += async (_, _) => await HandleChangeAsync(dbPath);
        _watcher.EnableRaisingEvents = true;

        // Surveille aussi le fichier WAL si la base utilise ce mode journal
        var walName = Path.GetFileName(dbPath) + "-wal";
        _walWatcher = new FileSystemWatcher(dir, walName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _walWatcher.Changed += async (_, _) => await HandleChangeAsync(dbPath);
        _walWatcher.EnableRaisingEvents = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var next = now.Date.AddDays(1).AddTicks(1);
            var delay = next - now;
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            if (!string.IsNullOrWhiteSpace(_config.Endpoint))
            {
                try
                {
                    await backupService.BackupAsync(_config.Endpoint);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreur lors de la sauvegarde distante programmée");
                }
            }

            if (!string.IsNullOrWhiteSpace(_driveConfig.Token))
            {
                try
                {
                    await gitHub.UploadBackupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreur lors de l'envoi sur GitHub");
                }
            }
        }
    }

    public async Task HandleChangeAsync(string dbPath, CancellationToken ct = default)
    {
        var bakPath = dbPath + ".bak";
        try
        {
            using (var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False"))
            using (var dest = new SqliteConnection($"Data Source={bakPath};Pooling=False"))
            {
                source.Open();
                dest.Open();
                source.BackupDatabase(dest);
            }

            if (!string.IsNullOrWhiteSpace(_config.Endpoint))
            {
                try
                {
                    await backupService.BackupAsync(_config.Endpoint);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreur lors de la sauvegarde distante immédiate");
                }
            }

            if (!string.IsNullOrWhiteSpace(_driveConfig.Token))
            {
                try
                {
                    await gitHub.UploadBackupAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreur lors de l'envoi sur GitHub");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la sauvegarde de la base de données");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _watcher?.Dispose();
        _walWatcher?.Dispose();
    }
}
