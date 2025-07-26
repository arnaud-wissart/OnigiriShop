using Microsoft.Data.Sqlite;

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
            {
                File.Copy(tempPath, endpoint, overwrite: true);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
