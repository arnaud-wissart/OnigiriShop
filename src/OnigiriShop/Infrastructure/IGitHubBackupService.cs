namespace OnigiriShop.Infrastructure;

/// <summary>
/// Abstraction du service de sauvegarde sur GitHub.
/// </summary>
public interface IGitHubBackupService
{
    Task UploadBackupAsync(CancellationToken ct = default);
    Task<bool> DownloadBackupAsync(string destinationPath, CancellationToken ct = default);
}