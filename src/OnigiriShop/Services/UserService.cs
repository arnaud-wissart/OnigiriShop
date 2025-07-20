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
    public class UserService(ISqliteConnectionFactory connectionFactory)
    {
        private static void AddParam(IDbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        public async Task<User> GetByIdAsync(int userId)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT Id, Email, Name, Phone, CreatedAt, IsActive, Role FROM User WHERE Id = @userId";
            var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { userId });
            return user;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, string name, string phone)
        {
            using var conn = connectionFactory.CreateConnection();

            // Vérifier si un autre utilisateur possède déjà ce nom
            var sqlCheck = "SELECT COUNT(*) FROM User WHERE Name = @name AND Id <> @userId";
            var count = await conn.ExecuteScalarAsync<int>(sqlCheck, new { name, userId });
            if (count > 0)
                throw new InvalidOperationException("Ce nom est déjà utilisé par un autre utilisateur.");

            var sql = @"UPDATE User SET Name = @name, Phone = @phone WHERE Id = @userId";
            return await conn.ExecuteAsync(sql, new { name, phone, userId }) > 0;
        }

        public void SoftDeleteUser(int userId)
        {
            using var conn = connectionFactory.CreateConnection();
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

        public List<User> GetAllUsers(string search = null)
        {
            using var conn = connectionFactory.CreateConnection();
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

        public async Task UpdateUserAsync(User user)
        {
            using var conn = connectionFactory.CreateConnection();
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
    }
}