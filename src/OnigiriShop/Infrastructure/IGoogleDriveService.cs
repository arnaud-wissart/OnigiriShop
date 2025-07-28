namespace OnigiriShop.Infrastructure
{
    /// <summary>
    /// Abstraction du service d'envoi des sauvegardes vers Google Drive.
    /// </summary>
    public interface IGoogleDriveService
    {
        Task<bool> DownloadBackupAsync(string folderId, string destinationPath, CancellationToken ct = default);
    }
}
