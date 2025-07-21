using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using Serilog;
using System.Data.Common;

namespace OnigiriShop.Services
{
    public class UserService(ISqliteConnectionFactory connectionFactory)
    {
        private const string AuditSql = @"INSERT INTO AuditLog (UserId, Action, TargetType, TargetId, Timestamp, Details)
                                          VALUES (@UserId, @Action, 'User', @TargetId, @Timestamp, @Details);";
        public async Task<User?> GetByIdAsync(int userId)
        {
            using var conn = connectionFactory.CreateConnection();
            await ((DbConnection)conn).OpenAsync();

            var sql = "SELECT Id, Email, Name, Phone, CreatedAt, IsActive, Role FROM User WHERE Id = @userId";
            var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { userId });
            return user;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, string name, string phone)
        {
            using var conn = connectionFactory.CreateConnection();
            await ((DbConnection)conn).OpenAsync();

            // Vérifier si un autre utilisateur possède déjà ce nom
            var sqlCheck = "SELECT COUNT(*) FROM User WHERE Name = @name AND Id <> @userId";
            var count = await conn.ExecuteScalarAsync<int>(sqlCheck, new { name, userId });
            if (count > 0)
                throw new InvalidOperationException("Ce nom est déjà utilisé par un autre utilisateur.");

            var sql = @"UPDATE User SET Name = @name, Phone = @phone WHERE Id = @userId";
            return await conn.ExecuteAsync(sql, new { name, phone, userId }) > 0;
        }

        public async Task SoftDeleteUserAsync(int userId)
        {
            using var conn = connectionFactory.CreateConnection();
            const string sql = "UPDATE User SET IsActive = 0 WHERE Id = @userId";
            await conn.ExecuteAsync(sql, new { userId });

            await conn.ExecuteAsync(AuditSql, new
            {
                UserId = (int?)null,
                Action = "SoftDelete",
                TargetId = userId,
                Timestamp = DateTime.UtcNow,
                Details = "Suppression logique (désactivation)"
            });
        }

        public async Task<List<User>> GetAllUsersAsync(string? search)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT Id, Email, Name, Phone, CreatedAt, IsActive, Role FROM User";
            object? param = null;
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " WHERE Email LIKE @search OR Name LIKE @search";
                param = new { search = "%" + search + "%" };
            }
            var users = await conn.QueryAsync<User>(sql, param);
            return users.ToList();
        }

        public async Task SetUserActiveAsync(int userId, bool isActive)
        {
            using var conn = connectionFactory.CreateConnection();
            const string sql = "UPDATE User SET IsActive = @isActive WHERE Id = @userId";
            await conn.ExecuteAsync(sql, new { isActive = isActive ? 1 : 0, userId });

            await conn.ExecuteAsync(AuditSql, new
            {
                UserId = (int?)null,
                Action = isActive ? "Activate" : "Deactivate",
                TargetId = userId,
                Timestamp = DateTime.UtcNow,
                Details = isActive ? "Compte activé" : "Compte désactivé"
            });

            Log.Information("Changement état actif pour userId={UserId}: {NewState}", userId, isActive ? "Actif" : "Inactif");
        }

        public async Task UpdateUserAsync(User user)
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using var txn = conn.BeginTransaction();

            const string sql = @"UPDATE User
        SET Email = @Email,
            Name = @Name,
            Phone = @Phone,
            IsActive = @IsActive,
            Role = @Role
        WHERE Id = @Id";

            await conn.ExecuteAsync(sql, new
            {
                user.Email,
                Name = user.Name ?? user.Email,
                Phone = user.Phone ?? string.Empty,
                IsActive = user.IsActive ? 1 : 0,
                Role = string.IsNullOrEmpty(user.Role) ? AuthConstants.RoleUser : user.Role,
                user.Id
            }, txn);

            await conn.ExecuteAsync(AuditSql, new
            {
                UserId = (int?)null,
                Action = "Update",
                TargetId = user.Id,
                Timestamp = DateTime.UtcNow,
                Details = $"Mise à jour de l'utilisateur {user.Email}"
            }, txn);

            txn.Commit();
        }
    }
}