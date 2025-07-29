using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Octokit;

namespace OnigiriShop.Infrastructure;

/// <summary>
/// Service chargé de sauvegarder la base dans un dépôt GitHub.
/// </summary>
public class GitHubBackupService : IGitHubBackupService
{
    private readonly IGitHubClient _client;
    private readonly GitHubBackupConfig _config;
    private readonly ILogger<GitHubBackupService> _logger;

    public GitHubBackupService(IOptions<GitHubBackupConfig> options, ILogger<GitHubBackupService> logger)
    {
        _config = options.Value;
        _logger = logger;
        var product = new ProductHeaderValue("OnigiriShop");
        _client = new GitHubClient(product)
        {
            Credentials = new Credentials(_config.Token)
        };
    }

    internal GitHubBackupService(IGitHubClient client, IOptions<GitHubBackupConfig> options, ILogger<GitHubBackupService> logger)
    {
        _client = client;
        _config = options.Value;
        _logger = logger;
    }

    public async Task UploadBackupAsync(CancellationToken ct = default)
    {
        var dbPath = DatabasePaths.GetPath();
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False"))
            using (var dest = new SqliteConnection($"Data Source={tempPath};Pooling=False"))
            {
                source.Open();
                dest.Open();
                source.BackupDatabase(dest);
            }

            var bytes = await File.ReadAllBytesAsync(tempPath, ct);
            var base64 = Convert.ToBase64String(bytes);

            try
            {
                var contents = await _client.Repository.Content.GetAllContentsByRef(_config.Owner, _config.Repo, _config.FilePath, _config.Branch);
                var sha = contents[0].Sha;
                var update = new UpdateFileRequest("Update database", base64, sha, _config.Branch);
                await _client.Repository.Content.UpdateFile(_config.Owner, _config.Repo, _config.FilePath, update);
            }
            catch (NotFoundException)
            {
                var create = new CreateFileRequest("Add database", base64, _config.Branch);
                await _client.Repository.Content.CreateFile(_config.Owner, _config.Repo, _config.FilePath, create);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public async Task<bool> DownloadBackupAsync(string destinationPath, CancellationToken ct = default)
    {
        try
        {
            var contents = await _client.Repository.Content.GetAllContentsByRef(_config.Owner, _config.Repo, _config.FilePath, _config.Branch);
            var content = contents[0];
            var bytes = Convert.FromBase64String(content.Content);
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(destinationPath, bytes, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du téléchargement depuis GitHub");
            return false;
        }
    }
}
