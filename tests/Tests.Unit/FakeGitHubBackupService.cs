using OnigiriShop.Infrastructure;

namespace Tests.Unit;

public class FakeGitHubBackupService : IGitHubBackupService
{
    public bool Uploaded { get; private set; }
    public int UploadCallCount { get; private set; }

    public Task UploadBackupAsync(CancellationToken ct = default)
    {
        Uploaded = true;
        UploadCallCount++;
        return Task.CompletedTask;
    }

    public Task<bool> DownloadBackupAsync(string destinationPath, CancellationToken ct = default)
    {
        File.WriteAllText(destinationPath, string.Empty);
        return Task.FromResult(true);
    }
}
