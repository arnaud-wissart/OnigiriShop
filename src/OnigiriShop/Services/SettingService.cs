using Dapper;
using OnigiriShop.Data.Interfaces;

namespace OnigiriShop.Services;

public class SettingService(ISqliteConnectionFactory connectionFactory)
{
    public async Task<string?> GetValueAsync(string key)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>("SELECT Value FROM Setting WHERE Key=@key", new { key });
    }

    public async Task SetValueAsync(string key, string value)
    {
        using var conn = connectionFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Setting WHERE Key=@key", new { key });
        if (count > 0)
            await conn.ExecuteAsync("UPDATE Setting SET Value=@value WHERE Key=@key", new { key, value });
        else
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES (@key, @value)", new { key, value });
    }
}
