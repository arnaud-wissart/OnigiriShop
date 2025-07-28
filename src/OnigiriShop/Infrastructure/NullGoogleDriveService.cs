namespace OnigiriShop.Infrastructure
{
    /// <summary>
    /// Implémentation inactive du service Google Drive utilisée
    /// lorsque le chemin des crédentials n'est pas fourni.
    /// </summary>
    public class NullGoogleDriveService : IGoogleDriveService
    {
        public Task UploadBackupAsync(string folderId, CancellationToken ct = default)
        {
            // Aucun envoi réalisé.
            return Task.CompletedTask;
        }
    }
}
