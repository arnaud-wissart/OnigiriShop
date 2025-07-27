using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Services;

public class MaintenanceService(IWebHostEnvironment env, IMigrationRunner runner)
{
    private readonly IWebHostEnvironment _env = env;
    private readonly IMigrationRunner _runner = runner;

    public IEnumerable<string?> GetLogFiles()
    {
        return Directory.GetFiles(_env.ContentRootPath, "*.log")
            .Select(Path.GetFileName)
            .OrderBy(f => f);
    }

    public async Task<string> ReadLogAsync(string fileName, int maxLines = 200)
    {
        var path = Path.Combine(_env.ContentRootPath, fileName);
        if (!File.Exists(path))
            return string.Empty;
        var lines = new List<string>();
        await using (var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        using (var reader = new StreamReader(stream))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
                lines.Add(line);
        }

        if (maxLines > 0 && lines.Count > maxLines)
            lines = lines.Skip(Math.Max(0, lines.Count - maxLines)).ToList();
        return string.Join('\n', lines);
    }

    public async Task<byte[]> GetDatabaseBytesAsync()
    {
        var dbPath = DatabasePaths.GetPath();
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False"))
            using (var dest = new SqliteConnection($"Data Source={tempPath};Pooling=False"))
            {
                source.Open();
                dest.Open();
                source.BackupDatabase(dest);
            }
            return await File.ReadAllBytesAsync(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public async Task ReplaceDatabaseAsync(Stream stream)
    {
        var dbPath = DatabasePaths.GetPath();
        DatabaseInitializer.DeleteDatabase(dbPath);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        using (var fs = new FileStream(dbPath, FileMode.Create, FileAccess.Write))
            await stream.CopyToAsync(fs);

        _runner.MigrateUp();
        var schemaPath = Path.Combine(_env.ContentRootPath, "SQL", "init_db.sql");
        var expectedHash = DatabaseInitializer.ComputeSchemaHash(schemaPath);
        DatabaseInitializer.SetSchemaHash(dbPath, expectedHash);
    }

    public DateTime? GetLastBackupDate()
    {
        var path = DatabasePaths.GetBackupPath();
        return File.Exists(path) ? File.GetLastWriteTime(path) : null;
    }

    public async Task RestoreLastBackupAsync()
    {
        var backupPath = DatabasePaths.GetBackupPath();
        if (!File.Exists(backupPath))
            return;

        await using var stream = new FileStream(
                    backupPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
        await ReplaceDatabaseAsync(stream);
    }
}
