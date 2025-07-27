namespace OnigiriShop.Infrastructure
{
    /// <summary>
    /// Abstraction du service d'envoi des sauvegardes vers Google Drive.
    /// </summary>
    public interface IGoogleDriveService
    {
        Task UploadBackupAsync(string folderId, CancellationToken ct = default);
    }
}
