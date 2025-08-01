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

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private const int RetryAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan DeduplicationDelay = TimeSpan.FromSeconds(1);
    private DateTime _lastHandledChange = DateTime.MinValue;
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private readonly object _debounceLock = new();
    private string? _dbPath;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _dbPath = DatabasePaths.GetPath();
        var dir = Path.GetDirectoryName(_dbPath) ?? ".";
        _watcher = new FileSystemWatcher(dir, Path.GetFileName(_dbPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += OnFileChanged;
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

    private void OnFileChanged(object? sender, FileSystemEventArgs e)
    {
        if (_dbPath is null)
            return;
        lock (_debounceLock)
        {
            _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(async _ => await HandleChangeAsync(_dbPath), null, DeduplicationDelay, Timeout.InfiniteTimeSpan);
        }
    }

    public async Task HandleChangeAsync(string dbPath, CancellationToken ct = default)
    {
        var bakPath = dbPath + ".bak";
        await _semaphore.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            if (now - _lastHandledChange < DeduplicationDelay)
                return;

            for (var attempt = 1; attempt <= RetryAttempts; attempt++)
            {
                try
                {
                    using var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False;Cache=Shared");
                    using var dest = new SqliteConnection($"Data Source={bakPath};Pooling=False");
                    source.Open();
                    using var cmd = source.CreateCommand();
                    cmd.CommandText = "PRAGMA busy_timeout=5000;";
                    cmd.ExecuteNonQuery();

                    dest.Open();
                    using var destCmd = dest.CreateCommand();
                    destCmd.CommandText = "PRAGMA busy_timeout=5000;";
                    destCmd.ExecuteNonQuery();

                    source.BackupDatabase(dest);
                    break;
                }
                catch (SqliteException ex) when ((ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6) && attempt < RetryAttempts)
                {
                    logger.LogWarning(ex, "Base verrouillée, tentative {Attempt}/{Max}", attempt, RetryAttempts);
                    await Task.Delay(RetryDelay, ct);
                }
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
            _lastHandledChange = now;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la sauvegarde de la base de données");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
    }
}
