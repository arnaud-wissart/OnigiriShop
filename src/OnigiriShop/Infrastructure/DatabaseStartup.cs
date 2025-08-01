using FluentMigrator.Runner;
using Microsoft.Extensions.Options;
using Serilog;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Fournit la méthode d'initialisation complète de la base de données au démarrage.
/// </summary>
public static class DatabaseStartup
{
    /// <summary>
    /// Restaure la base au besoin depuis les sauvegardes puis applique les migrations.
    /// </summary>
    /// <param name="services">Provider de services.</param>
    /// <param name="dbPath">Chemin du fichier SQLite.</param>
    /// <param name="expectedHash">Version attendue du schéma initial.</param>
    public static void RestoreAndMigrateDatabase(this IServiceProvider services, string dbPath, uint expectedHash)
    {
        using var scope = services.CreateScope();
        var githubCfg = scope.ServiceProvider.GetRequiredService<IOptions<GitHubBackupConfig>>().Value;
        var backupCfg = scope.ServiceProvider.GetRequiredService<IOptions<BackupConfig>>().Value;
        var httpBackup = scope.ServiceProvider.GetRequiredService<HttpDatabaseBackupService>();
        IGitHubBackupService? github = string.IsNullOrWhiteSpace(githubCfg.Token)
            ? null
            : scope.ServiceProvider.GetRequiredService<IGitHubBackupService>();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        var restored = false;
        var localExists = File.Exists(dbPath);
        uint localVersion = localExists ? DatabaseInitializer.GetSchemaVersion(dbPath) : 0;

        var remoteTemp = string.Empty;
        var remoteExists = false;
        uint remoteVersion = 0;

        if (github is not null)
        {
            remoteTemp = Path.GetTempFileName();
            remoteExists = github.DownloadBackupAsync(remoteTemp).GetAwaiter().GetResult();
            if (remoteExists && SqliteHelper.IsSqliteDatabase(remoteTemp))
                remoteVersion = DatabaseInitializer.GetSchemaVersion(remoteTemp);
            else
                remoteExists = false;
        }

        if (remoteExists && (!localExists || remoteVersion > localVersion))
        {
            DatabaseInitializer.DeleteDatabase(dbPath);
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Move(remoteTemp, dbPath, true);
            restored = true;
        }
        else if (!restored && !string.IsNullOrWhiteSpace(backupCfg.Endpoint))
        {
            restored = httpBackup.RestoreAsync(backupCfg.Endpoint, dbPath).GetAwaiter().GetResult();
            if (!restored)
                Log.Warning("Aucun backup valide n'a été trouvé à {Endpoint}", backupCfg.Endpoint);
        }

        if (File.Exists(remoteTemp))
            File.Delete(remoteTemp);

        DatabaseInitializer.EnsureDatabaseValid(dbPath);
        runner.MigrateUp();

        if (!DatabaseInitializer.IsSchemaUpToDate(dbPath, expectedHash))
        {
            DatabaseInitializer.SetSchemaHash(dbPath, expectedHash);
            var version = DatabaseInitializer.GetSchemaVersion(dbPath);
            Log.Information("Schema initialisé avec la version {Version}", version);
        }
    }
}
