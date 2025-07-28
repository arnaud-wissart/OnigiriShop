using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Service chargé d'envoyer une sauvegarde de la base dans un dossier Google Drive.
/// </summary>
public class GoogleDriveService(IOptions<DriveConfig> config) : IGoogleDriveService
{
    private readonly DriveService _service = new(new BaseClientService.Initializer
    {
        HttpClientInitializer = GoogleCredential.FromFile(config.Value.CredentialsPath)
            .CreateScoped(DriveService.Scope.DriveFile),
        ApplicationName = "OnigiriShop"
    });

    /// <summary>
    /// Charge la base de données dans le dossier Google Drive indiqué.
    /// </summary>
    public async Task UploadBackupAsync(string folderId, CancellationToken ct = default)
    {
        var dbPath = DatabasePaths.GetPath();
        var temp = Path.GetTempFileName();
        try
        {
            using (var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False"))
            using (var dest = new SqliteConnection($"Data Source={temp};Pooling=False"))
            {
                source.Open();
                dest.Open();
                source.BackupDatabase(dest);
            }

            var meta = new Google.Apis.Drive.v3.Data.File
            {
                Name = Path.GetFileName(dbPath),
                Parents = new[] { folderId }
            };

            await using var fs = new FileStream(temp, FileMode.Open, FileAccess.Read);
            var request = _service.Files.Create(meta, fs, "application/x-sqlite3");
            request.Fields = "id";
            await request.UploadAsync(ct);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    /// <summary>
    /// Télécharge la dernière sauvegarde présente dans le dossier spécifié.
    /// </summary>
    public async Task<bool> DownloadBackupAsync(string folderId, string destinationPath, CancellationToken ct = default)
    {
        var request = _service.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false";
        request.Fields = "files(id, name, createdTime)";
        var files = await request.ExecuteAsync(ct);
        var file = files.Files
            .Where(f => f.Name == Path.GetFileName(destinationPath))
            .OrderByDescending(f => f.CreatedTimeDateTimeOffset)
            .FirstOrDefault();
        if (file == null)
            return false;

        var temp = Path.GetTempFileName();
        try
        {
            await using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write))
            {
                var get = _service.Files.Get(file.Id);
                await get.DownloadAsync(fs, ct);
            }

            if (!SqliteHelper.IsSqliteDatabase(temp))
                return false;

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(temp, destinationPath, true);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }
}
