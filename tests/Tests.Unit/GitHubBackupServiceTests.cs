using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;
using OnigiriShop.Infrastructure;

namespace Tests.Unit;

public class GitHubBackupServiceTests : IDisposable
{
    private readonly string _dbPath = Path.GetTempFileName();
    private readonly SqliteConnection _conn;

    public GitHubBackupServiceTests()
    {
        _conn = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        _conn.Open();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE T (Id INTEGER PRIMARY KEY); INSERT INTO T VALUES (1);";
        cmd.ExecuteNonQuery();

        var servicePath = DatabasePaths.GetPath();
        var dir = Path.GetDirectoryName(servicePath)!;
        Directory.CreateDirectory(dir);
        File.Copy(_dbPath, servicePath, true);
    }

    [Fact]
    public async Task UploadBackupAsync_DoesNotThrow_WhenDatabaseOpen()
    {
        var contentsMock = new Mock<IRepositoryContentsClient>();
        contentsMock
            .Setup(c => c.GetAllContentsByRef(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new NotFoundException("not found", System.Net.HttpStatusCode.NotFound));
        contentsMock
            .Setup(c => c.CreateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateFileRequest>()))
            .ReturnsAsync(new RepositoryContentChangeSet());

        var repoMock = new Mock<IRepositoriesClient>();
        repoMock.SetupGet(r => r.Content).Returns(contentsMock.Object);

        var clientMock = new Mock<IGitHubClient>();
        clientMock.SetupGet(c => c.Repository).Returns(repoMock.Object);

        var options = Options.Create(new GitHubBackupConfig());
        var svc = new GitHubBackupService(clientMock.Object, options, NullLogger<GitHubBackupService>.Instance);
        var ex = await Record.ExceptionAsync(() => svc.UploadBackupAsync());
        Assert.Null(ex);
    }

    public void Dispose()
    {
        _conn.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        Environment.SetEnvironmentVariable("ONIGIRISHOP_DB_PATH", null);
    }
}
