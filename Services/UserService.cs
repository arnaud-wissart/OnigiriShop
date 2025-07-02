using OnigiriShop.Data;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using Serilog;
using System.Security.Cryptography;

namespace OnigiriShop.Services
{
    public class UserService
    {
        private readonly ISqliteConnectionFactory _connectionFactory;
        private readonly EmailService _emailService;
        private readonly AllowedAdminsManager _adminsManager;
        public UserService(ISqliteConnectionFactory connectionFactory, EmailService emailService, AllowedAdminsManager adminsManager)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
            _adminsManager = adminsManager;
        }

        public async Task InviteUserAsync(string email, string name, string siteBaseUrl, int expiryMinutes = 60)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            // 1. Création de l'utilisateur (IsActive=0, pas de mdp)
            var cmdUser = conn.CreateCommand();
            cmdUser.CommandText = @"
            INSERT INTO User (Email, Name, CreatedAt, IsActive, Role)
            VALUES (@Email, @Name, @CreatedAt, 0, 'User');
            SELECT last_insert_rowid();";
            cmdUser.Parameters.AddWithValue("@Email", email);
            cmdUser.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? email : name);
            cmdUser.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            var userId = Convert.ToInt32(cmdUser.ExecuteScalar());

            // 2. Génération du token magic link (usage unique, sécurisé)
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var cmdToken = conn.CreateCommand();
            cmdToken.CommandText = @"
            INSERT INTO MagicLinkToken (UserId, Token, Expiry, CreatedAt)
            VALUES (@UserId, @Token, @Expiry, @CreatedAt);";
            cmdToken.Parameters.AddWithValue("@UserId", userId);
            cmdToken.Parameters.AddWithValue("@Token", token);
            cmdToken.Parameters.AddWithValue("@Expiry", expiry);
            cmdToken.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            cmdToken.ExecuteNonQuery();

            // 3. Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
            INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
            VALUES (@UserId, 'Invite', 'User', @TargetId, @Timestamp, @Details);";
            cmdAudit.Parameters.AddWithValue("@UserId", DBNull.Value); // AdminId si disponible
            cmdAudit.Parameters.AddWithValue("@TargetId", userId);
            cmdAudit.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
            cmdAudit.Parameters.AddWithValue("@Details", $"Invitation envoyée à {email}");
            cmdAudit.ExecuteNonQuery();

            // 4. Génération du lien d'invitation
            var inviteUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

            // 5. Envoi de l'email d'invitation (log inclus)
            await _emailService.SendUserInvitationAsync(email, name, inviteUrl);

            Log.Information("Invitation envoyée à {Email}, userId={UserId}, token={Token}", email, userId, token);
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
            cmdUser.Parameters.AddWithValue("@Hash", hash);
            cmdUser.Parameters.AddWithValue("@Salt", saltBase64);
            cmdUser.Parameters.AddWithValue("@UserId", userId);
            cmdUser.ExecuteNonQuery();

            // Marque le token comme utilisé
            var cmdToken = conn.CreateCommand();
            cmdToken.CommandText = @"
                UPDATE MagicLinkToken SET UsedAt = @Now WHERE Token = @Token";
            cmdToken.Parameters.AddWithValue("@Token", token);
            cmdToken.Parameters.AddWithValue("@Now", DateTime.UtcNow);
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
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);

            var userId = cmd.ExecuteScalar();

            return Task.FromResult(userId != null ? Convert.ToInt32(userId) : 0);
        }
        public void SoftDeleteUser(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE User SET IsActive = 0 WHERE Id = @UserId";
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();

            // Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, 'SoftDelete', 'User', @TargetId, @Timestamp, @Details);";
            cmdAudit.Parameters.AddWithValue("@UserId", DBNull.Value);
            cmdAudit.Parameters.AddWithValue("@TargetId", userId);
            cmdAudit.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
            cmdAudit.Parameters.AddWithValue("@Details", "Suppression logique (désactivation)");
            cmdAudit.ExecuteNonQuery();
        }
        public Task<User> AuthenticateAsync(string email, string password)
        {
            // 1. Vérifie dans le fichier JSON d'abord
            if (_adminsManager.Validate(email, password))
            {
                return Task.FromResult(new User
                {
                    Id = 0,
                    Email = email,
                    Name = email,
                    Role = AuthConstants.RoleAdmin,
                    IsActive = true
                });
            }

            // 2. Sinon, comportement habituel : check DB
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Email, Name, PasswordHash, PasswordSalt, IsActive, Role FROM User WHERE Email = @Email AND IsActive = 1";
            cmd.Parameters.AddWithValue("@Email", email);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return Task.FromResult<User>(null);

            var hashDb = reader["PasswordHash"] as string;
            var saltDb = reader["PasswordSalt"] as string;

            if (string.IsNullOrEmpty(hashDb) || string.IsNullOrEmpty(saltDb))
                return Task.FromResult<User>(null); // User n'a pas encore choisi son mdp

            // Vérification du mot de passe
            var salt = Convert.FromBase64String(saltDb);
            using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
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
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
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
            cmdFind.Parameters.AddWithValue("@Email", email);
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
            cmdToken.Parameters.AddWithValue("@UserId", userId);
            cmdToken.Parameters.AddWithValue("@Token", token);
            cmdToken.Parameters.AddWithValue("@Expiry", expiry);
            cmdToken.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            cmdToken.ExecuteNonQuery();

            // 3. Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, 'ResetPassword', 'User', @TargetId, @Timestamp, @Details);";
            cmdAudit.Parameters.AddWithValue("@UserId", DBNull.Value);
            cmdAudit.Parameters.AddWithValue("@TargetId", userId);
            cmdAudit.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
            cmdAudit.Parameters.AddWithValue("@Details", $"Lien de réinitialisation envoyé à {email}");
            cmdAudit.ExecuteNonQuery();

            // 4. Génération du lien
            var resetUrl = $"{siteBaseUrl.TrimEnd('/')}/invite?token={Uri.EscapeDataString(token)}";

            // 5. Envoi email (tu peux créer un template spécial)
            await _emailService.SendUserInvitationAsync(email, name, resetUrl);
        }

        public void SetUserActive(int userId, bool isActive)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();

            // Update de l’état actif
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE User SET IsActive = @IsActive WHERE Id = @UserId";
            cmd.Parameters.AddWithValue("@IsActive", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();

            // Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, @Action, 'User', @TargetId, @Timestamp, @Details);";
            cmdAudit.Parameters.AddWithValue("@UserId", DBNull.Value); // Tu peux renseigner l’admin si tu l’as
            cmdAudit.Parameters.AddWithValue("@Action", isActive ? "Activate" : "Deactivate");
            cmdAudit.Parameters.AddWithValue("@TargetId", userId);
            cmdAudit.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
            cmdAudit.Parameters.AddWithValue("@Details", isActive ? "Compte activé" : "Compte désactivé");
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
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Name", user.Name ?? user.Email);
            cmd.Parameters.AddWithValue("@Phone", user.Phone ?? "");
            cmd.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@Role", string.IsNullOrEmpty(user.Role) ? AuthConstants.RoleUser : user.Role);
            cmd.Parameters.AddWithValue("@Id", user.Id);

            cmd.ExecuteNonQuery();

            // Audit log
            var cmdAudit = conn.CreateCommand();
            cmdAudit.CommandText = @"
        INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
        VALUES (@UserId, 'Update', 'User', @TargetId, @Timestamp, @Details);";
            cmdAudit.Parameters.AddWithValue("@UserId", DBNull.Value); // Admin à compléter si besoin
            cmdAudit.Parameters.AddWithValue("@TargetId", user.Id);
            cmdAudit.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
            cmdAudit.Parameters.AddWithValue("@Details", $"Mise à jour de l'utilisateur {user.Email}");
            cmdAudit.ExecuteNonQuery();

            txn.Commit();
            await Task.CompletedTask;
        }

    }
}