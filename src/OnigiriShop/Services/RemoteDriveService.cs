using Dapper;
using OnigiriShop.Data.Interfaces;

namespace OnigiriShop.Services;

/// <summary>
/// Service permettant de stocker l'identifiant du dossier Google Drive
/// destiné aux sauvegardes distantes.
/// </summary>
public class RemoteDriveService(ISqliteConnectionFactory connectionFactory)
{
    private const string Key = "DriveFolderId";
    private string? _cachedId;

    /// <summary>
    /// Récupère l'identifiant du dossier distant.
    /// </summary>
    public async Task<string?> GetFolderIdAsync()
    {
        if (_cachedId != null)
            return _cachedId;

        using var conn = connectionFactory.CreateConnection();
        _cachedId = await conn.QuerySingleOrDefaultAsync<string>(
            "SELECT Value FROM Setting WHERE Key = @key",
            new { key = Key });
        return _cachedId;
    }

    /// <summary>
    /// Enregistre l'identifiant du dossier distant.
    /// </summary>
    public async Task SetFolderIdAsync(string? folderId)
    {
        using var conn = connectionFactory.CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Setting WHERE Key = @key",
            new { key = Key });
        if (exists > 0)
            await conn.ExecuteAsync(
                "UPDATE Setting SET Value = @folderId WHERE Key = @key",
                new { folderId, key = Key });
        else
            await conn.ExecuteAsync(
                "INSERT INTO Setting (Key, Value) VALUES (@key, @folderId)",
                new { folderId, key = Key });

        _cachedId = folderId;
    }
}
