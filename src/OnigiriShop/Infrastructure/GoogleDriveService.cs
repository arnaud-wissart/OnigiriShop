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
}
