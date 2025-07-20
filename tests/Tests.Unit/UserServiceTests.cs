using Dapper;
using OnigiriShop.Data.Models;
using OnigiriShop.Services;

namespace Tests.Unit;

public class UserServiceTests
{
    [Fact]
    public async Task SoftDeleteUserAsync_DisablesUserAndLogs()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var service = new UserService(new FakeSqliteConnectionFactory(conn));

        await service.SoftDeleteUserAsync(1);

        var active = await conn.ExecuteScalarAsync<int>("SELECT IsActive FROM User WHERE Id = 1");
        Assert.Equal(0, active);
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AuditLog WHERE TargetId = 1 AND Action = 'SoftDelete'");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SetUserActiveAsync_UpdatesStateAndLogs()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var service = new UserService(new FakeSqliteConnectionFactory(conn));

        await service.SetUserActiveAsync(2, false);

        var active = await conn.ExecuteScalarAsync<int>("SELECT IsActive FROM User WHERE Id = 2");
        Assert.Equal(0, active);
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AuditLog WHERE TargetId = 2 AND Action = 'Deactivate'");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUserAndLogs()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var service = new UserService(new FakeSqliteConnectionFactory(conn));

        var user = new User { Id = 1, Email = "update@test.com", Name = "New", Phone = "000", IsActive = false, Role = "User" };
        await service.UpdateUserAsync(user);

        var result = await conn.QuerySingleAsync<User>("SELECT * FROM User WHERE Id = 1");
        Assert.Equal("update@test.com", result.Email);
        Assert.Equal("New", result.Name);
        Assert.Equal(0, result.IsActive ? 1 : 0);

        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AuditLog WHERE TargetId = 1 AND Action = 'Update'");
        Assert.Equal(1, count);
    }
}