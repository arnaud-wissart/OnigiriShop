using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class EmailTemplateService(ISqliteConnectionFactory connectionFactory)
    {
        public async Task<List<EmailTemplate>> GetAllAsync()
        {
            using var conn = connectionFactory.CreateConnection();
            var result = await conn.QueryAsync<EmailTemplate>("SELECT * FROM EmailTemplate ORDER BY Name");
            return result.AsList();
        }

        public async Task<EmailTemplate?> GetByNameAsync(string name)
        {
            using var conn = connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<EmailTemplate>("SELECT * FROM EmailTemplate WHERE Name=@name", new { name });
        }

        public async Task<int> CreateAsync(EmailTemplate template)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"INSERT INTO EmailTemplate (Name, HtmlContent, TextContent)
                        VALUES (@Name, @HtmlContent, @TextContent);
                        SELECT last_insert_rowid();";
            return await conn.ExecuteScalarAsync<int>(sql, template);
        }

        public async Task<bool> UpdateAsync(EmailTemplate template)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"UPDATE EmailTemplate
                        SET Name=@Name, HtmlContent=@HtmlContent, TextContent=@TextContent
                        WHERE Id=@Id";
            return await conn.ExecuteAsync(sql, template) > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            return await conn.ExecuteAsync("DELETE FROM EmailTemplate WHERE Id=@id", new { id }) > 0;
        }
    }
}