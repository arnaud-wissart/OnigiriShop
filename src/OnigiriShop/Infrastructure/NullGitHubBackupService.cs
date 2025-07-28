namespace OnigiriShop.Infrastructure
{
    /// <summary>
    /// Implémentation neutre utilisée quand aucun token GitHub n'est configuré.
    /// </summary>
    public class NullGitHubBackupService : IGitHubBackupService
    {
        public Task UploadBackupAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> DownloadBackupAsync(string destinationPath, CancellationToken ct = default)
            => Task.FromResult(false);
    }
}
