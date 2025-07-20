using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Infrastructure;
using System.Text.Json;

namespace OnigiriShop.Services;

public class UserPreferenceService(ISqliteConnectionFactory connectionFactory)
{
    public async Task<UserPreferences> GetUserPreferencesAsync(int userId)
    {
        using var conn = connectionFactory.CreateConnection();
        var prefsJson = await conn.QuerySingleOrDefaultAsync<string>(
            "SELECT Preferences FROM User WHERE Id = @userId", new { userId });
        if (string.IsNullOrWhiteSpace(prefsJson))
            return new UserPreferences();
        try
        {
            return JsonSerializer.Deserialize<UserPreferences>(prefsJson)
                   ?? new UserPreferences();
        }
        catch
        {
            return new UserPreferences();
        }
    }

    public async Task SaveUserPreferencesAsync(int userId, UserPreferences prefs)
    {
        var prefsJson = JsonSerializer.Serialize(prefs);
        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE User SET Preferences = @prefsJson WHERE Id = @userId",
            new { prefsJson, userId }
        );
    }
}
