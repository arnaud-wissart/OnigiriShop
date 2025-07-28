using OnigiriShop.Infrastructure;

namespace Tests.Unit
{
    public class FakeGoogleDriveService : IGoogleDriveService
    {
        public List<string> Uploaded { get; } = [];
        public Task UploadBackupAsync(string folderId, CancellationToken ct = default)
        {
            Uploaded.Add(folderId);
            return Task.CompletedTask;
        }

        public Task<bool> DownloadBackupAsync(string folderId, string destinationPath, CancellationToken ct = default)
        {
            // Simule une restauration réussie en écrivant un fichier vide
            File.WriteAllText(destinationPath, string.Empty);
            return Task.FromResult(true);
        }
    }
}
