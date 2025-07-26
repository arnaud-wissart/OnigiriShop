using OnigiriShop.Infrastructure;

namespace Tests.Unit;

public class DatabasePathsTests
{
    [Fact]
    public void GetPath_Returns_Path_And_Creates_Folder()
    {
        var path = DatabasePaths.GetPath();
        Assert.EndsWith(Path.Combine("BDD", "OnigiriShop.db"), path);
        var dir = Path.GetDirectoryName(path)!;
        Assert.True(Directory.Exists(dir));
    }

    [Fact]
    public void GetBackupPath_Returns_Path_With_Bak_Extension()
    {
        var path = DatabasePaths.GetBackupPath();
        Assert.EndsWith(Path.Combine("BDD", "OnigiriShop.bak"), path);
        var dir = Path.GetDirectoryName(path)!;
        Assert.True(Directory.Exists(dir));
    }

    [Fact]
    public void GetBackupPath_Uses_Environment_Variable()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var customPath = Path.Combine(tempDir, "mydb.db");
        Environment.SetEnvironmentVariable("ONIGIRISHOP_DB_PATH", customPath);

        try
        {
            var backup = DatabasePaths.GetBackupPath();
            Assert.Equal(Path.ChangeExtension(customPath, ".bak"), backup);
            var dir = Path.GetDirectoryName(backup)!;
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ONIGIRISHOP_DB_PATH", null);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}