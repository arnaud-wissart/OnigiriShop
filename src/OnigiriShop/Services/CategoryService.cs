using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services;

public class CategoryService(ISqliteConnectionFactory connectionFactory) : ICategoryService
{
    public async Task<List<Category>> GetAllAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        var result = await conn.QueryAsync<Category>("SELECT * FROM Category");
        return result.AsList();
    }

    public async Task<int> CreateAsync(Category c)
    {
        using var conn = connectionFactory.CreateConnection();
        var sql = "INSERT INTO Category (Name) VALUES (@Name); SELECT last_insert_rowid();";
        return await conn.ExecuteScalarAsync<int>(sql, c);
    }

    public async Task<bool> UpdateAsync(Category c)
    {
        using var conn = connectionFactory.CreateConnection();
        var sql = "UPDATE Category SET Name=@Name WHERE Id=@Id";
        return await conn.ExecuteAsync(sql, c) > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = connectionFactory.CreateConnection();
        var sql = "DELETE FROM Category WHERE Id=@id";
        return await conn.ExecuteAsync(sql, new { id }) > 0;
    }
}
