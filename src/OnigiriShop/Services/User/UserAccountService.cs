using Microsoft.Extensions.Options;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using Serilog;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace OnigiriShop.Services;

public class UserAccountService(ISqliteConnectionFactory connectionFactory, EmailService emailService, IOptions<MagicLinkConfig> magicLinkConfig)
{
    private readonly int _expiryMinutes = magicLinkConfig.Value.ExpiryMinutes;

    private static void AddParam(IDbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    public async Task InviteUserAsync(string email, string name, string siteBaseUrl)
    {
        using var conn = connectionFactory.CreateConnection();
        await ((DbConnection)conn).OpenAsync();

        var cmdCheck = conn.CreateCommand();
        cmdCheck.CommandText = "SELECT COUNT(*) FROM User WHERE Email = @Email";
        AddParam(cmdCheck, "@Email", email.Trim().ToLower());
        var exists = Convert.ToInt32(await ((DbCommand)cmdCheck).ExecuteScalarAsync()) > 0;
        if (exists)
            throw new InvalidOperationException("Un utilisateur avec cet email existe déjà.");

        var cmdUser = conn.CreateCommand();
        cmdUser.CommandText = @"INSERT INTO User (Email, Name, CreatedAt, IsActive, Role)
VALUES (@Email, @Name, @CreatedAt, 0, 'User');
SELECT last_insert_rowid();";
        AddParam(cmdUser, "@Email", email);
        AddParam(cmdUser, "@Name", string.IsNullOrWhiteSpace(name) ? email : name);
        AddParam(cmdUser, "@CreatedAt", DateTime.UtcNow);

        var userId = Convert.ToInt32(await ((DbCommand)cmdUser).ExecuteScalarAsync());

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expiry = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var cmdToken = conn.CreateCommand();
        cmdToken.CommandText = @"INSERT INTO MagicLinkToken (UserId, Token, Expiry, CreatedAt)
VALUES (@UserId, @Token, @Expiry, @CreatedAt);";
        AddParam(cmdToken, "@UserId", userId);
        AddParam(cmdToken, "@Token", token);
        AddParam(cmdToken, "@Expiry", expiry);
        AddParam(cmdToken, "@CreatedAt", DateTime.UtcNow);
        await ((DbCommand)cmdToken).ExecuteNonQueryAsync();

        var cmdAudit = conn.CreateCommand();
        cmdAudit.CommandText = @"INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
VALUES (@UserId, 'Invite', 'User', @TargetId, @Timestamp, @Details);";
        AddParam(cmdAudit, "@UserId", DBNull.Value);
        AddParam(cmdAudit, "@TargetId", userId);
        AddParam(cmdAudit, "@Timestamp", DateTime.UtcNow);
        AddParam(cmdAudit, "@Details", $"Invitation envoyée à {email}");
        await ((DbCommand)cmdAudit).ExecuteNonQueryAsync();

        var inviteUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

        await emailService.SendUserInvitationAsync(email, name, inviteUrl);

        Log.Information("Invitation envoyée à {Email}, userId={UserId}, token={Token}", email, userId, token);
    }

    public async Task<int> FindUserIdByTokenAsync(string token)
    {
        using var conn = connectionFactory.CreateConnection();
        await ((DbConnection)conn).OpenAsync();
        using var cmd = (DbCommand)conn.CreateCommand();
        cmd.CommandText = "SELECT UserId FROM MagicLinkToken WHERE Token = @Token";
        AddParam(cmd, "@Token", token);
        var userId = await cmd.ExecuteScalarAsync();
        return userId != null ? Convert.ToInt32(userId) : 0;
    }

    public async Task SetUserPasswordAsync(int userId, string password, string token)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(32);
        using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = Convert.ToBase64String(derive.GetBytes(64));
        var saltBase64 = Convert.ToBase64String(salt);

        using var conn = connectionFactory.CreateConnection();
        await ((DbConnection)conn).OpenAsync();
        using var txn = conn.BeginTransaction();

        var cmdUser = conn.CreateCommand();
        cmdUser.CommandText = @"UPDATE User SET PasswordHash = @Hash, PasswordSalt = @Salt, IsActive = 1 WHERE Id = @UserId";
        AddParam(cmdUser, "@Hash", hash);
        AddParam(cmdUser, "@Salt", saltBase64);
        AddParam(cmdUser, "@UserId", userId);
        await ((DbCommand)cmdUser).ExecuteNonQueryAsync();

        var cmdToken = conn.CreateCommand();
        cmdToken.CommandText = @"UPDATE MagicLinkToken SET UsedAt = @Now WHERE Token = @Token";
        AddParam(cmdToken, "@Token", token);
        AddParam(cmdToken, "@Now", DateTime.UtcNow);
        await ((DbCommand)cmdToken).ExecuteNonQueryAsync();

        await ((DbTransaction)txn).CommitAsync();
        return;
    }

    public async Task<int> ValidateInviteTokenAsync(string token)
    {
        using var conn = connectionFactory.CreateConnection();
        await ((DbConnection)conn).OpenAsync();
        using var cmd = (DbCommand)conn.CreateCommand();
        cmd.CommandText = @"SELECT UserId FROM MagicLinkToken
WHERE Token = @Token AND UsedAt IS NULL AND Expiry >= @Now";
        AddParam(cmd, "@Token", token);
        AddParam(cmd, "@Now", DateTime.UtcNow);
        var userId = await cmd.ExecuteScalarAsync();
        return userId != null ? Convert.ToInt32(userId) : 0;
    }

    public async Task<User> AuthenticateAsync(string email, string password)
    {
        using var conn = connectionFactory.CreateConnection();
        await ((DbConnection)conn).OpenAsync();
        using var cmd = (DbCommand)conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Email, Name, PasswordHash, PasswordSalt, IsActive, Role FROM User WHERE Email = @Email AND IsActive = 1";
        AddParam(cmd, "@Email", email);
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        var hashDb = reader["PasswordHash"] as string;
        var saltDb = reader["PasswordSalt"] as string;

        if (string.IsNullOrEmpty(hashDb) || string.IsNullOrEmpty(saltDb))
            return null;

        var salt = Convert.FromBase64String(saltDb);
        using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hashInput = Convert.ToBase64String(derive.GetBytes(64));

        if (!SlowEquals(hashDb, hashInput))
            return null;

        return new User
        {
            Id = Convert.ToInt32(reader["Id"]),
            Email = reader["Email"].ToString(),
            Name = reader["Name"].ToString(),
            Role = reader["Role"].ToString(),
            IsActive = true
        };
    }

    private static bool SlowEquals(string a, string b)
    {
        var diff = (uint)a.Length ^ (uint)b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
            diff |= (uint)a[i] ^ b[i];
        return diff == 0;
    }

    public async Task GenerateAndSendResetLinkAsync(string email, string name, string siteBaseUrl, int expiryMinutes = 60)
    {
        using var conn = connectionFactory.CreateConnection();
        await ((DbConnection)conn).OpenAsync();

        var cmdFind = (DbCommand)conn.CreateCommand();
        cmdFind.CommandText = "SELECT Id FROM User WHERE Email = @Email";
        AddParam(cmdFind, "@Email", email);
        var userIdObj = await cmdFind.ExecuteScalarAsync();
        if (userIdObj == null)
            throw new Exception("Utilisateur non trouvé");

        int userId = Convert.ToInt32(userIdObj);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var cmdToken = (DbCommand)conn.CreateCommand();
        cmdToken.CommandText = @"INSERT INTO MagicLinkToken (UserId, Token, Expiry, CreatedAt)
VALUES (@UserId, @Token, @Expiry, @CreatedAt);";
        AddParam(cmdToken, "@UserId", userId);
        AddParam(cmdToken, "@Token", token);
        AddParam(cmdToken, "@Expiry", expiry);
        AddParam(cmdToken, "@CreatedAt", DateTime.UtcNow);
        await cmdToken.ExecuteNonQueryAsync();

        var cmdAudit = (DbCommand)conn.CreateCommand();
        cmdAudit.CommandText = @"INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
VALUES (@UserId, 'ResetPassword', 'User', @TargetId, @Timestamp, @Details);";
        AddParam(cmdAudit, "@UserId", DBNull.Value);
        AddParam(cmdAudit, "@TargetId", userId);
        AddParam(cmdAudit, "@Timestamp", DateTime.UtcNow);
        AddParam(cmdAudit, "@Details", $"Lien de réinitialisation envoyé à {email}");
        await cmdAudit.ExecuteNonQueryAsync();

        var resetUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

        await emailService.SendPasswordResetAsync(email, name, resetUrl);
    }
}