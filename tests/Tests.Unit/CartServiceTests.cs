using OnigiriShop.Services;
using OnigiriShop.Data.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Tests.Unit
{
    public class CartServiceSqlTests
    {
        private class SqliteTestConnectionFactory(string connectionString) : ISqliteConnectionFactory
        {
            public IDbConnection CreateConnection()
            {
                var conn = new SqliteConnection(connectionString);
                conn.Open();
                return conn;
            }
        }

        private readonly string _schema = @"
            CREATE TABLE IF NOT EXISTS Cart (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                DateCreated DATETIME NOT NULL,
                DateUpdated DATETIME NOT NULL,
                IsActive BOOLEAN NOT NULL
            );
            CREATE TABLE IF NOT EXISTS CartItem (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CartId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                DateAdded DATETIME NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Product (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                Price REAL NOT NULL,
                IsOnMenu BOOLEAN NOT NULL,
                ImagePath TEXT,
                IsDeleted BOOLEAN NOT NULL DEFAULT 0
            );";

        private CartService MakeCartService(SqliteConnection conn)
        {
            // Tu peux ici injecter le ProductService si tu veux tester les jointures aussi
            return new CartService(new SqliteTestConnectionFactory(conn.ConnectionString));
        }

        private async Task<SqliteConnection> InitDbAsync()
        {
            var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();
            await conn.ExecuteAsync(_schema);
            await conn.ExecuteAsync("INSERT INTO Product (Name, Description, Price, IsOnMenu, ImagePath, IsDeleted) VALUES ('Test', 'desc', 3.5, 1, '', 0);");
            return conn;
        }

        [Fact]
        public async Task AddItemAsync_Ajoute_Produit()
        {
            var conn = await InitDbAsync();
            var cartService = MakeCartService(conn);

            await cartService.AddItemAsync(1, 1, 2); // userId=1, productId=1, qty=2

            var cart = await cartService.GetActiveCartAsync(1);
            Assert.NotNull(cart);
            Assert.Single(cart.Items);
            Assert.Equal(2, cart.Items.First().Quantity);
            Assert.Equal(1, cart.Items.First().ProductId);
        }

        [Fact]
        public async Task RemoveItemAsync_Decremente_Produit_Ou_Supprime()
        {
            var conn = await InitDbAsync();
            var cartService = MakeCartService(conn);

            await cartService.AddItemAsync(1, 1, 2);
            await cartService.RemoveItemAsync(1, 1, 1);

            var cart = await cartService.GetActiveCartAsync(1);
            Assert.Single(cart.Items);
            Assert.Equal(1, cart.Items.First().Quantity);

            await cartService.RemoveItemAsync(1, 1, 1);
            cart = await cartService.GetActiveCartAsync(1);
            Assert.Empty(cart.Items);
        }

        [Fact]
        public async Task ClearCartAsync_Vide_Le_Panier()
        {
            var conn = await InitDbAsync();
            var cartService = MakeCartService(conn);

            await cartService.AddItemAsync(1, 1, 2);
            await cartService.ClearCartAsync(1);

            var cart = await cartService.GetActiveCartAsync(1);
            Assert.Empty(cart.Items);
        }

        // Ajoute d'autres tests pour GetCartItemsWithProductsAsync, etc.
    }
}
