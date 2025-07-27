using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OnigiriShop.Services;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Service en arrière-plan chargé de créer une copie .bak lorsque la base change.
/// </summary>
public class DatabaseBackupBackgroundService(
    ILogger<DatabaseBackupBackgroundService> logger,
    HttpDatabaseBackupService backupService,
    RemoteDriveService driveService,
    IGoogleDriveService googleDrive,
    IOptions<BackupConfig> options) : BackgroundService
{
    private readonly BackupConfig _config = options.Value;
    private FileSystemWatcher? _watcher;

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

            var folderId = await driveService.GetFolderIdAsync();
            if (!string.IsNullOrWhiteSpace(folderId))
            {
                try
                {
                    await googleDrive.UploadBackupAsync(folderId, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreur lors de l'envoi sur Google Drive");
                }
            }
        }
    }

    public Task HandleChangeAsync(string dbPath)
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
            logger.LogError(ex, "Erreur lors de la sauvegarde de la base de données");
        }
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        base.Dispose();
        _watcher?.Dispose();
    }
}
