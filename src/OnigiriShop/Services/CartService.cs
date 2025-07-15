using Dapper;
using OnigiriShop.Data.Models;
using OnigiriShop.Data.Interfaces;
using System.Data;

namespace OnigiriShop.Services
{
    public class CartService(ISqliteConnectionFactory connectionFactory)
    {
        private static (IDbConnection Conn, bool Owns) GetOrCreateConnection(IDbConnection connection, ISqliteConnectionFactory factory)
        {
            if (connection != null)
                return (connection, false); // Connexion gérée à l'extérieur
            return (factory.CreateConnection(), true); // Connexion créée ici, à disposer
        }

        public async Task<Cart> GetActiveCartAsync(int userId, IDbConnection connection = null)
        {
            var (conn, owns) = GetOrCreateConnection(connection, connectionFactory);
            try
            {
                var cart = await conn.QueryFirstOrDefaultAsync<Cart>(
                    "SELECT * FROM Cart WHERE UserId = @UserId AND IsActive = 1 ORDER BY DateUpdated DESC LIMIT 1", new { UserId = userId });

                if (cart != null)
                {
                    var items = await conn.QueryAsync<CartItem>("SELECT * FROM CartItem WHERE CartId = @CartId", new { CartId = cart.Id });
                    cart.Items = items.ToList();
                }

                return cart;
            }
            finally
            {
                if (owns)
                    conn.Dispose();
            }
        }

        public async Task<Cart> CreateOrGetActiveCartAsync(int userId, IDbConnection connection = null)
        {
            var cart = await GetActiveCartAsync(userId, connection);
            if (cart != null) return cart;

            var (conn, owns) = GetOrCreateConnection(connection, connectionFactory);
            try
            {
                var now = DateTime.UtcNow;
                var cartId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Cart (UserId, DateCreated, DateUpdated, IsActive) VALUES (@UserId, @Now, @Now, 1);
                      SELECT last_insert_rowid();",
                    new { UserId = userId, Now = now });

                return new Cart
                {
                    Id = (int)cartId,
                    UserId = userId,
                    DateCreated = now,
                    DateUpdated = now,
                    IsActive = true,
                    Items = []
                };
            }
            finally
            {
                if (owns)
                    conn.Dispose();
            }
        }

        public async Task AddItemAsync(int userId, int productId, int quantity, IDbConnection connection = null)
        {
            var cart = await CreateOrGetActiveCartAsync(userId, connection);

            var (conn, owns) = GetOrCreateConnection(connection, connectionFactory);
            try
            {
                var existing = await conn.QueryFirstOrDefaultAsync<CartItem>(
                    "SELECT * FROM CartItem WHERE CartId = @CartId AND ProductId = @ProductId",
                    new { CartId = cart.Id, ProductId = productId });

                if (existing == null)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO CartItem (CartId, ProductId, Quantity, DateAdded)
                          VALUES (@CartId, @ProductId, @Quantity, @Now)",
                        new { CartId = cart.Id, ProductId = productId, Quantity = quantity, Now = DateTime.UtcNow });
                }
                else
                {
                    await conn.ExecuteAsync(
                        @"UPDATE CartItem SET Quantity = Quantity + @Quantity WHERE Id = @Id",
                        new { Quantity = quantity, Id = existing.Id });
                }

                await conn.ExecuteAsync("UPDATE Cart SET DateUpdated = @Now WHERE Id = @Id", new { Now = DateTime.UtcNow, Id = cart.Id });
            }
            finally
            {
                if (owns)
                    conn.Dispose();
            }
        }

        public async Task RemoveItemAsync(int userId, int productId, int quantity, IDbConnection connection = null)
        {
            var cart = await GetActiveCartAsync(userId, connection);
            if (cart == null) return;

            var (conn, owns) = GetOrCreateConnection(connection, connectionFactory);
            try
            {
                var existing = await conn.QueryFirstOrDefaultAsync<CartItem>(
                    "SELECT * FROM CartItem WHERE CartId = @CartId AND ProductId = @ProductId",
                    new { CartId = cart.Id, ProductId = productId });

                if (existing == null) return;
                if (existing.Quantity <= quantity)
                {
                    await conn.ExecuteAsync("DELETE FROM CartItem WHERE Id = @Id", new { Id = existing.Id });
                }
                else
                {
                    await conn.ExecuteAsync("UPDATE CartItem SET Quantity = Quantity - @Quantity WHERE Id = @Id",
                        new { Quantity = quantity, Id = existing.Id });
                }

                await conn.ExecuteAsync("UPDATE Cart SET DateUpdated = @Now WHERE Id = @Id", new { Now = DateTime.UtcNow, Id = cart.Id });
            }
            finally
            {
                if (owns)
                    conn.Dispose();
            }
        }

        public async Task ClearCartAsync(int userId, IDbConnection connection = null)
        {
            var cart = await GetActiveCartAsync(userId, connection);
            if (cart == null) return;

            var (conn, owns) = GetOrCreateConnection(connection, connectionFactory);
            try
            {
                await conn.ExecuteAsync("DELETE FROM CartItem WHERE CartId = @CartId", new { CartId = cart.Id });
                await conn.ExecuteAsync("UPDATE Cart SET DateUpdated = @Now WHERE Id = @Id", new { Now = DateTime.UtcNow, Id = cart.Id });
            }
            finally
            {
                if (owns)
                    conn.Dispose();
            }
        }

        public async Task<List<CartItem>> GetCartItemsAsync(int userId, IDbConnection connection = null)
        {
            var cart = await GetActiveCartAsync(userId, connection);
            if (cart == null) return [];
            var (conn, owns) = GetOrCreateConnection(connection, connectionFactory);
            try
            {
                var items = await conn.QueryAsync<CartItem>("SELECT * FROM CartItem WHERE CartId = @CartId", new { CartId = cart.Id });
                return items.ToList();
            }
            finally
            {
                if (owns)
                    conn.Dispose();
            }
        }

        public async Task<List<CartItemWithProduct>> GetCartItemsWithProductsAsync(int userId, IDbConnection connection = null)
        {
            var cart = await GetActiveCartAsync(userId, connection);
            if (cart == null) return [];
            var (conn, owns) = GetOrCreateConnection(connection, connectionFactory);
            try
            {
                var sql = @"
            SELECT ci.*, 
                   p.Id, p.Name, p.Description, p.Price, p.IsOnMenu, p.ImagePath, p.IsDeleted
            FROM CartItem ci
            INNER JOIN Product p ON ci.ProductId = p.Id
            WHERE ci.CartId = @CartId";
                var results = await conn.QueryAsync<CartItemWithProduct, Product, CartItemWithProduct>(
                    sql,
                    (ci, p) => { ci.Product = p; return ci; },
                    new { CartId = cart.Id }
                );
                return results.ToList();
            }
            finally
            {
                if (owns)
                    conn.Dispose();
            }
        }
    }
}
