using Dapper;
using Microsoft.Extensions.Options;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Services;

public class SiteNameService(ISqliteConnectionFactory connectionFactory, IOptions<SiteConfig> siteConfig)
{
    private readonly ISqliteConnectionFactory _connectionFactory = connectionFactory;
    private readonly string _defaultName = siteConfig.Value.Name;
    private string? _cachedName;

    public string GetSiteName()
    {
        if (_cachedName != null)
            return _cachedName;
        using var conn = _connectionFactory.CreateConnection();
        var name = conn.QuerySingleOrDefault<string>("SELECT Value FROM Setting WHERE Key = 'SiteName'");
        _cachedName = name ?? _defaultName;
        return _cachedName;
    }

    public async Task<string> GetSiteNameAsync()
    {
        if (_cachedName != null)
            return _cachedName;
        using var conn = _connectionFactory.CreateConnection();
        var name = await conn.QuerySingleOrDefaultAsync<string>("SELECT Value FROM Setting WHERE Key = 'SiteName'");
        _cachedName = name ?? _defaultName;
        return _cachedName;
    }

    public async Task SetSiteNameAsync(string name)
    {
        using var conn = _connectionFactory.CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Setting WHERE Key = 'SiteName'");
        if (exists > 0)
            await conn.ExecuteAsync("UPDATE Setting SET Value = @name WHERE Key = 'SiteName'", new { name });
        else
            await conn.ExecuteAsync("INSERT INTO Setting (Key, Value) VALUES ('SiteName', @name)", new { name });

        _cachedName = name;
    }
}
