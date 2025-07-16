using Dapper;
using Microsoft.Extensions.Options;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using Serilog;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;

namespace OnigiriShop.Services
{
    public class UserService
    {
        private readonly ISqliteConnectionFactory _connectionFactory;
        private readonly EmailService _emailService;
        private readonly int _expiryMinutes;

        public UserService(ISqliteConnectionFactory connectionFactory, EmailService emailService, IOptions<MagicLinkConfig> magicLinkConfig)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
            _expiryMinutes = magicLinkConfig.Value.ExpiryMinutes;
        }

        // --------- AJOUTER CETTE MÉTHODE ---------
        private static void AddParam(IDbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        public async Task InviteUserAsync(string email, string name, string siteBaseUrl)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            var cmdCheck = conn.CreateCommand();
            cmdCheck.CommandText = "SELECT COUNT(*) FROM User WHERE Email = @Email";
            AddParam(cmdCheck, "@Email", email.Trim().ToLower());
            var exists = Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0;
            if (exists)
                throw new InvalidOperationException("Un utilisateur avec cet email existe déjà.");

            // 1. Création de l'utilisateur (IsActive=0, pas de mdp)
            var cmdUser = conn.CreateCommand();
            cmdUser.CommandText = @"
            INSERT INTO User (Email, Name, CreatedAt, IsActive, Role)
            VALUES (@Email, @Name, @CreatedAt, 0, 'User');
            SELECT last_insert_rowid();";
            AddParam(cmdUser, "@Email", email);
            AddParam(cmdUser, "@Name", string.IsNullOrWhiteSpace(name) ? email : name);
            AddParam(cmdUser, "@CreatedAt", DateTime.UtcNow);

            var userId = Convert.ToInt32(cmdUser.ExecuteScalar());

            // 2. Génération du token magic link (usage unique, sécurisé)
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var expiry = DateTime.UtcNow.AddMinutes(_expiryMinutes);

            var cmdToken = conn.CreateCommand();
            cmdToken.CommandText = @"
            INSERT INTO MagicLinkToken (UserId, Token, Expiry, CreatedAt)
            VALUES (@UserId, @Token, @Expiry, @CreatedAt);";
            AddParam(cmdToken, "@UserId", userId);
            AddParam(cmdToken, "@Token", token);
            AddParam(cmdToken, "@Expiry", expiry);
            AddParam(cmdToken, "@CreatedAt", DateTime.UtcNow);
            cmdToken.ExecuteNonQuery();

            // 3. Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
            INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
            VALUES (@UserId, 'Invite', 'User', @TargetId, @Timestamp, @Details);";
            AddParam(cmdAudit, "@UserId", DBNull.Value); // AdminId si disponible
            AddParam(cmdAudit, "@TargetId", userId);
            AddParam(cmdAudit, "@Timestamp", DateTime.UtcNow);
            AddParam(cmdAudit, "@Details", $"Invitation envoyée à {email}");
            cmdAudit.ExecuteNonQuery();

            // 4. Génération du lien d'invitation
            var inviteUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

            // 5. Envoi de l'email d'invitation (log inclus)
            await _emailService.SendUserInvitationAsync(email, name, inviteUrl);

            Log.Information("Invitation envoyée à {Email}, userId={UserId}, token={Token}", email, userId, token);
        }

        public Task<int> FindUserIdByToken(string token)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UserId FROM MagicLinkToken WHERE Token = @Token";
            AddParam(cmd, "@Token", token);
            var userId = cmd.ExecuteScalar();
            return userId != null ? Task.FromResult(Convert.ToInt32(userId)) : Task.FromResult(0);
        }
        public async Task<User> GetByIdAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT Id, Email, Name, Phone, CreatedAt, IsActive, Role FROM User WHERE Id = @userId";
            var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { userId });
            return user;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, string name, string phone)
        {
            using var conn = _connectionFactory.CreateConnection();

            // Vérifier si un autre utilisateur possède déjà ce nom
            var sqlCheck = "SELECT COUNT(*) FROM User WHERE Name = @name AND Id <> @userId";
            var count = await conn.ExecuteScalarAsync<int>(sqlCheck, new { name, userId });
            if (count > 0)
                throw new InvalidOperationException("Ce nom est déjà utilisé par un autre utilisateur.");

            var sql = @"UPDATE User SET Name = @name, Phone = @phone WHERE Id = @userId";
            return await conn.ExecuteAsync(sql, new { name, phone, userId }) > 0;
        }


        public Task SetUserPasswordAsync(int userId, string password, string token)
        {
            // Génère un sel
            byte[] salt = RandomNumberGenerator.GetBytes(32);
            // Hash du mot de passe
            using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hash = Convert.ToBase64String(derive.GetBytes(64));
            var saltBase64 = Convert.ToBase64String(salt);

            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var txn = conn.BeginTransaction();

            // Update User
            var cmdUser = conn.CreateCommand();
            cmdUser.CommandText = @"
                UPDATE User SET PasswordHash = @Hash, PasswordSalt = @Salt, IsActive = 1 WHERE Id = @UserId";
            AddParam(cmdUser, "@Hash", hash);
            AddParam(cmdUser, "@Salt", saltBase64);
            AddParam(cmdUser, "@UserId", userId);
            cmdUser.ExecuteNonQuery();

            // Marque le token comme utilisé
            var cmdToken = conn.CreateCommand();
            cmdToken.CommandText = @"
                UPDATE MagicLinkToken SET UsedAt = @Now WHERE Token = @Token";
            AddParam(cmdToken, "@Token", token);
            AddParam(cmdToken, "@Now", DateTime.UtcNow);
            cmdToken.ExecuteNonQuery();

            txn.Commit();
            // Envoi de mail de confirmation possible ici
            return Task.CompletedTask;
        }

        public Task<int> ValidateInviteTokenAsync(string token)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT UserId FROM MagicLinkToken
                WHERE Token = @Token AND UsedAt IS NULL AND Expiry >= @Now";
            AddParam(cmd, "@Token", token);
            AddParam(cmd, "@Now", DateTime.UtcNow);

            var userId = cmd.ExecuteScalar();

            return Task.FromResult(userId != null ? Convert.ToInt32(userId) : 0);
        }
        public void SoftDeleteUser(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE User SET IsActive = 0 WHERE Id = @UserId";
            AddParam(cmd, "@UserId", userId);
            cmd.ExecuteNonQuery();

            // Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, 'SoftDelete', 'User', @TargetId, @Timestamp, @Details);";
            AddParam(cmdAudit, "@UserId", DBNull.Value);
            AddParam(cmdAudit, "@TargetId", userId);
            AddParam(cmdAudit, "@Timestamp", DateTime.UtcNow);
            AddParam(cmdAudit, "@Details", "Suppression logique (désactivation)");
            cmdAudit.ExecuteNonQuery();
        }
        public Task<User> AuthenticateAsync(string email, string password)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Email, Name, PasswordHash, PasswordSalt, IsActive, Role FROM User WHERE Email = @Email AND IsActive = 1";
            AddParam(cmd, "@Email", email);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return Task.FromResult<User>(null);

            var hashDb = reader["PasswordHash"] as string;
            var saltDb = reader["PasswordSalt"] as string;

            if (string.IsNullOrEmpty(hashDb) || string.IsNullOrEmpty(saltDb))
                return Task.FromResult<User>(null); // User n'a pas encore choisi son mdp

            // Vérification du mot de passe
            var salt = Convert.FromBase64String(saltDb);
            using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hashInput = Convert.ToBase64String(derive.GetBytes(64));

            if (!SlowEquals(hashDb, hashInput))
                return Task.FromResult<User>(null);

            // Utilisateur authentifié
            return Task.FromResult(new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Email = reader["Email"].ToString(),
                Name = reader["Name"].ToString(),
                Role = reader["Role"].ToString(),
                IsActive = true
            });
        }

        // Protection timing attack
        private static bool SlowEquals(string a, string b)
        {
            var diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)a[i] ^ b[i];
            return diff == 0;
        }



        public List<User> GetAllUsers(string search = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Email, Name, Phone, CreatedAt, IsActive, Role FROM User";
            if (!string.IsNullOrWhiteSpace(search))
            {
                cmd.CommandText += " WHERE Email LIKE @Search OR Name LIKE @Search";
                AddParam(cmd, "@Search", "%" + search + "%");
            }
            using var reader = cmd.ExecuteReader();
            var users = new List<User>();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Email = reader["Email"].ToString(),
                    Name = reader["Name"].ToString(),
                    Phone = reader["Phone"]?.ToString() ?? "",
                    CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                    Role = reader["Role"].ToString()
                });
            }
            return users;
        }

        public async Task GenerateAndSendResetLinkAsync(string email, string name, string siteBaseUrl, int expiryMinutes = 60)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            // 1. Trouver le user
            var cmdFind = conn.CreateCommand();
            cmdFind.CommandText = "SELECT Id FROM User WHERE Email = @Email";
            AddParam(cmdFind, "@Email", email);
            var userIdObj = cmdFind.ExecuteScalar();
            if (userIdObj == null)
                throw new Exception("Utilisateur non trouvé");

            int userId = Convert.ToInt32(userIdObj);

            // 2. Générer un token magic link
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var cmdToken = conn.CreateCommand();
            cmdToken.CommandText = @"
        INSERT INTO MagicLinkToken (UserId, Token, Expiry, CreatedAt)
        VALUES (@UserId, @Token, @Expiry, @CreatedAt);";
            AddParam(cmdToken, "@UserId", userId);
            AddParam(cmdToken, "@Token", token);
            AddParam(cmdToken, "@Expiry", expiry);
            AddParam(cmdToken, "@CreatedAt", DateTime.UtcNow);
            cmdToken.ExecuteNonQuery();

            // 3. Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, 'ResetPassword', 'User', @TargetId, @Timestamp, @Details);";
            AddParam(cmdAudit, "@UserId", DBNull.Value);
            AddParam(cmdAudit, "@TargetId", userId);
            AddParam(cmdAudit, "@Timestamp", DateTime.UtcNow);
            AddParam(cmdAudit, "@Details", $"Lien de réinitialisation envoyé à {email}");
            cmdAudit.ExecuteNonQuery();

            // 4. Génération du lien
            var resetUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

            // 5. Envoi email
            await _emailService.SendPasswordResetAsync(email, name, resetUrl);
        }

        public void SetUserActive(int userId, bool isActive)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            // Update de l’état actif
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE User SET IsActive = @IsActive WHERE Id = @UserId";
            AddParam(cmd, "@IsActive", isActive ? 1 : 0);
            AddParam(cmd, "@UserId", userId);
            cmd.ExecuteNonQuery();

            // Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, @Action, 'User', @TargetId, @Timestamp, @Details);";
            AddParam(cmdAudit, "@UserId", DBNull.Value); // Tu peux renseigner l’admin si tu l’as
            AddParam(cmdAudit, "@Action", isActive ? "Activate" : "Deactivate");
            AddParam(cmdAudit, "@TargetId", userId);
            AddParam(cmdAudit, "@Timestamp", DateTime.UtcNow);
            AddParam(cmdAudit, "@Details", isActive ? "Compte activé" : "Compte désactivé");
            cmdAudit.ExecuteNonQuery();

            Log.Information("Changement état actif pour userId={UserId}: {NewState}", userId, isActive ? "Actif" : "Inactif");
        }

        public async Task UpdateUserAsync(User user)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var txn = conn.BeginTransaction();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE User
        SET Email = @Email,
            Name = @Name,
            Phone = @Phone,
            IsActive = @IsActive,
            Role = @Role
        WHERE Id = @Id";
            AddParam(cmd, "@Email", user.Email);
            AddParam(cmd, "@Name", user.Name ?? user.Email);
            AddParam(cmd, "@Phone", user.Phone ?? "");
            AddParam(cmd, "@IsActive", user.IsActive ? 1 : 0);
            AddParam(cmd, "@Role", string.IsNullOrEmpty(user.Role) ? AuthConstants.RoleUser : user.Role);
            AddParam(cmd, "@Id", user.Id);

            cmd.ExecuteNonQuery();

            // Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, 'Update', 'User', @TargetId, @Timestamp, @Details);";
            AddParam(cmdAudit, "@UserId", DBNull.Value); // Admin à compléter si besoin
            AddParam(cmdAudit, "@TargetId", user.Id);
            AddParam(cmdAudit, "@Timestamp", DateTime.UtcNow);
            AddParam(cmdAudit, "@Details", $"Mise à jour de l'utilisateur {user.Email}");
            cmdAudit.ExecuteNonQuery();

            txn.Commit();
            await Task.CompletedTask;
        }
        public async Task<UserPreferences> GetUserPreferencesAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
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
                // Cas d'un JSON cassé, on évite un crash
                return new UserPreferences();
            }
        }

        public async Task SaveUserPreferencesAsync(int userId, UserPreferences prefs)
        {
            var prefsJson = JsonSerializer.Serialize(prefs);
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE User SET Preferences = @prefsJson WHERE Id = @userId",
                new { prefsJson, userId }
            );
        }
    }
}
