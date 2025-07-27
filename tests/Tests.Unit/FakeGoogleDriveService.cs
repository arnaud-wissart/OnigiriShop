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
    }
}
