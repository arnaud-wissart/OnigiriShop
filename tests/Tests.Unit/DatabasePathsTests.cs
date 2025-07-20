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
}