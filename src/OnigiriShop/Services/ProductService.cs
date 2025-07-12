using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class ProductService
    {
        private readonly ISqliteConnectionFactory _connectionFactory;

        public ProductService(ISqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<Product>> GetAllAsync(bool includeDeleted = false)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Product" + (includeDeleted ? "" : " WHERE IsDeleted = 0");
            var result = await conn.QueryAsync<Product>(sql);
            return result.AsList();
        }

        public async Task<List<Product>> GetMenuProductsAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Product WHERE IsDeleted = 0 AND IsOnMenu = 1";
            var result = await conn.QueryAsync<Product>(sql);
            return result.AsList();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Product WHERE Id = @id AND IsDeleted = 0";
            return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { id });
        }

        public async Task<int> CreateAsync(Product p)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Product (Name, Description, Price, IsOnMenu, ImagePath, IsDeleted)
                        VALUES (@Name, @Description, @Price, @IsOnMenu, @ImagePath, 0);
                        SELECT last_insert_rowid();";
            return await conn.ExecuteScalarAsync<int>(sql, p);
        }

        public async Task<bool> UpdateAsync(Product p)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Product
                        SET Name=@Name, Description=@Description, Price=@Price,
                            IsOnMenu=@IsOnMenu, ImagePath=@ImagePath
                        WHERE Id=@Id AND IsDeleted=0";
            return await conn.ExecuteAsync(sql, p) > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Product SET IsDeleted=1 WHERE Id=@id";
            return await conn.ExecuteAsync(sql, new { id }) > 0;
        }
    }
}
