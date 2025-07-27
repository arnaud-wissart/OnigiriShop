using Microsoft.Data.Sqlite;
using System.Text;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Service de sauvegarde de la base de données par HTTP ou copie locale.
/// </summary>
public class HttpDatabaseBackupService(HttpClient client)
{
    private readonly HttpClient _client = client;

    /// <summary>
    /// Sauvegarde la base de données vers l'endpoint fourni.
    /// </summary>
    /// <param name="endpoint">URL HTTP ou chemin local de destination.</param>
    public async Task BackupAsync(string endpoint)
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

            if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                await using var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
                using var content = new StreamContent(fs);
                var response = await _client.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
            }
            else
                File.Copy(tempPath, endpoint, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Restaure la base de données à partir de l'endpoint fourni.
    /// </summary>
    /// <param name="endpoint">URL HTTP ou chemin local source.</param>
    /// <param name="destinationPath">Chemin local de la base à restaurer.</param>
    /// <returns>True si la restauration a réussi.</returns>
    public async Task<bool> RestoreAsync(string endpoint, string destinationPath)
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _client.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode)
                    return false;
                await using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fs);
            }
            else
            {
                if (!File.Exists(endpoint))
                    return false;
                File.Copy(endpoint, tempPath, true);
            }

            if (!IsSqliteDatabase(tempPath))
                return false;

            using (var conn = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly;Pooling=False"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                var result = (string?)cmd.ExecuteScalar();
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(tempPath, destinationPath, true);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static bool IsSqliteDatabase(string path)
    {
        const string header = "SQLite format 3\0";
        var buffer = new byte[header.Length];
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            if (fs.Read(buffer, 0, buffer.Length) != buffer.Length)
                return false;
            return Encoding.ASCII.GetString(buffer) == header;
        }
        catch
        {
            return false;
        }
    }
}
