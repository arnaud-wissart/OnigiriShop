using Dapper;
using Microsoft.Extensions.Options;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using Serilog;
using System.Security.Cryptography;

namespace OnigiriShop.Services;

public class UserAccountService(ISqliteConnectionFactory connectionFactory, EmailService emailService, IOptions<MagicLinkConfig> magicLinkConfig)
{
    private readonly int _expiryMinutes = magicLinkConfig.Value.ExpiryMinutes;

    private const string InsertTokenSql = @"INSERT INTO MagicLinkToken (UserId, Token, Expiry, CreatedAt)
VALUES (@UserId, @Token, @Expiry, @CreatedAt);";
    private const string AuditSql = @"INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
VALUES (@UserId, @Action, 'User', @TargetId, @Timestamp, @Details);";

    public async Task InviteUserAsync(string email, string name, string siteBaseUrl)
    {
        using var conn = connectionFactory.CreateConnection();
        var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM User WHERE Email = @Email",
                    new { Email = email.Trim().ToLower() });
        if (exists > 0)
            throw new InvalidOperationException("Un utilisateur avec cet email existe déjà.");

        var userSql = @"INSERT INTO User (Email, Name, CreatedAt, IsActive, Role)
VALUES (@Email, @Name, @CreatedAt, 0, 'User');
SELECT last_insert_rowid();";
        var userId = await conn.ExecuteScalarAsync<int>(userSql, new
        {
            Email = email,
            Name = string.IsNullOrWhiteSpace(name) ? email : name,
            CreatedAt = DateTime.UtcNow
        });

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expiry = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        await conn.ExecuteAsync(InsertTokenSql, new
        {
            UserId = userId,
            Token = token,
            Expiry = expiry,
            CreatedAt = DateTime.UtcNow
        });

        await conn.ExecuteAsync(AuditSql, new
        {
            UserId = (int?)null,
            Action = "Invite",
            TargetId = userId,
            Timestamp = DateTime.UtcNow,
            Details = $"Invitation envoyée à {email}"
        });

        var inviteUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

        await emailService.SendUserInvitationAsync(email, name, inviteUrl);

        Log.Information("Invitation envoyée à {Email}, userId={UserId}, token={Token}", email, userId, token);
    }

    public async Task<int> FindUserIdByTokenAsync(string token)
    {
        using var conn = connectionFactory.CreateConnection();
        var userId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT UserId FROM MagicLinkToken WHERE Token = @Token",
                    new { Token = token });

        return userId ?? 0;
    }

    public async Task SetUserPasswordAsync(int userId, string password, string token)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(32);
        using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = Convert.ToBase64String(derive.GetBytes(64));
        var saltBase64 = Convert.ToBase64String(salt);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();

        using var txn = conn.BeginTransaction();

        await conn.ExecuteAsync(
                    "UPDATE User SET PasswordHash = @Hash, PasswordSalt = @Salt, IsActive = 1 WHERE Id = @UserId",
                    new { Hash = hash, Salt = saltBase64, UserId = userId }, txn);

        await conn.ExecuteAsync(
                    "UPDATE MagicLinkToken SET UsedAt = @Now WHERE Token = @Token",
                    new { Token = token, Now = DateTime.UtcNow }, txn);

        txn.Commit();
        return;
    }

    public async Task<int> ValidateInviteTokenAsync(string token)
    {
        using var conn = connectionFactory.CreateConnection();
        var userId = await conn.ExecuteScalarAsync<int?>(@"SELECT UserId FROM MagicLinkToken
WHERE Token = @Token AND UsedAt IS NULL AND Expiry >= @Now", new { Token = token, Now = DateTime.UtcNow });
        return userId ?? 0;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        using var conn = connectionFactory.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM User WHERE Email = @Email AND IsActive = 1",
            new { Email = email });

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt))
            return null;

        var salt = Convert.FromBase64String(user.PasswordSalt);
        using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hashInput = Convert.ToBase64String(derive.GetBytes(64));

        if (!SlowEquals(user.PasswordHash, hashInput))
            return null;

        return new User
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
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
        var userIdObj = await conn.ExecuteScalarAsync<int?>("SELECT Id FROM User WHERE Email = @Email", new { Email = email });
        if (userIdObj is null)
            throw new Exception("Utilisateur non trouvé");

        var userId = userIdObj.Value;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        await conn.ExecuteAsync(InsertTokenSql, new
        {
            UserId = userId,
            Token = token,
            Expiry = expiry,
            CreatedAt = DateTime.UtcNow
        });

        await conn.ExecuteAsync(AuditSql, new
        {
            UserId = (int?)null,
            Action = "ResetPassword",
            TargetId = userId,
            Timestamp = DateTime.UtcNow,
            Details = $"Lien de réinitialisation envoyé à {email}"
        });

        var resetUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

        await emailService.SendPasswordResetAsync(email, name, resetUrl);
    }
}