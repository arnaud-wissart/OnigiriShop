using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Services
{
    public class EmailVariationService(ISqliteConnectionFactory connectionFactory)
    {
        public async Task<List<EmailVariation>> GetByTypeAsync(string type)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT * FROM EmailVariation WHERE Type = @Type";
            var result = await conn.QueryAsync<EmailVariation>(sql, new { Type = type });
            return result.AsList();
        }

        public async Task<List<EmailVariation>> GetAllAsync()
        {
            using var conn = connectionFactory.CreateConnection();
            var result = await conn.QueryAsync<EmailVariation>("SELECT * FROM EmailVariation ORDER BY Type, Name");
            return result.AsList();
        }

        public async Task<int> CreateAsync(EmailVariation variation)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"INSERT INTO EmailVariation (Type, Name, Value, Extra)
                        VALUES (@Type, @Name, @Value, @Extra);
                        SELECT last_insert_rowid();";
            return await conn.ExecuteScalarAsync<int>(sql, variation);
        }

        public async Task<bool> UpdateAsync(EmailVariation variation)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"UPDATE EmailVariation
                        SET Type=@Type, Name=@Name, Value=@Value, Extra=@Extra
                        WHERE Id=@Id";
            return await conn.ExecuteAsync(sql, variation) > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "DELETE FROM EmailVariation WHERE Id=@id";
            return await conn.ExecuteAsync(sql, new { id }) > 0;
        }

        public async Task<EmailVariation?> GetRandomByTypeAsync(string type)
        {
            var list = await GetByTypeAsync(type);
            if (list.Count == 0) return null;
            var rand = new Random();
            return list[rand.Next(list.Count)];
        }

        public async Task<string?> GetRandomValueByTypeAsync(string type)
        {
            var list = await GetByTypeAsync(type);
            if (list.Count == 0) return null;
            var rand = new Random();
            return list[rand.Next(list.Count)].Value;
        }

        public async Task<(string? Email, string? Name)> GetRandomExpeditorAsync()
        {
            var exp = await GetRandomByTypeAsync("Expeditor");
            return exp == null ? (null, null) : (exp.Value, exp.Extra);
        }
    }
}
