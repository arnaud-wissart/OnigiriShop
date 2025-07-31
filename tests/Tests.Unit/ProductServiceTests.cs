using OnigiriShop.Data.Models;
using OnigiriShop.Services;
using Microsoft.Data.Sqlite;

namespace Tests.Unit
{
    public class ProductServiceTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly IProductService _service;

        public ProductServiceTests()
        {
            // 1. Connexion ouverte et persistante pour toute la durée du test
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();

            // 2. Création du schéma
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE Product (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                Description TEXT,
                Price REAL,
                CategoryId INTEGER DEFAULT 1,
                IsOnMenu INTEGER,
                ImageBase64 TEXT,
                IsDeleted INTEGER
            );";
            cmd.ExecuteNonQuery();

            // 3. Factory in-memory qui retourne TOUJOURS la même connexion
            var factory = new FakeSqliteConnectionFactory(_conn);
            _service = new ProductService(factory);
        }

        public void Dispose() => _conn.Dispose();

        [Fact]
        public async Task CreateAsync_CreeUnProduit()
        {
            var product = new Product
            {
                Name = "Onigiri",
                Description = "Riz Japonais",
                Price = 3.5m,
                IsOnMenu = true,
                ImageBase64 = "onigiri",
                IsDeleted = false
            };

            // Act
            int id = await _service.CreateAsync(product);

            // Assert
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Product WHERE Id = @id";
            cmd.Parameters.Add(new SqliteParameter("@id", id));
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetByIdAsync_RetourneProduit()
        {
            // Seed un produit
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Product (Name, Description, Price, CategoryId, IsOnMenu, ImageBase64, IsDeleted)
                                    VALUES ('Test', 'Desc', 1.99, 1, 1, 'img.png', 0)";
                cmd.ExecuteNonQuery();
            }

            var product = await _service.GetByIdAsync(1);
            Assert.NotNull(product);
            Assert.Equal("Test", product.Name);
        }

        [Fact]
        public async Task UpdateAsync_ModifieLeProduit()
        {
            // Seed un produit
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Product (Name, Description, Price, CategoryId, IsOnMenu, ImageBase64, IsDeleted)
                                    VALUES ('Test', 'Desc', 1.99, 1, 1, 'img.png', 0)";
                cmd.ExecuteNonQuery();
            }

            var updatedProduct = new Product
            {
                Id = 1,
                Name = "Test2",
                Description = "Nouvelle desc",
                Price = 4.5m,
                IsOnMenu = false,
                ImageBase64 = "nouveau"
            };

            var result = await _service.UpdateAsync(updatedProduct);
            Assert.True(result);

            var product = await _service.GetByIdAsync(1);
            Assert.NotNull(product);
            Assert.Equal("Test2", product.Name);
            Assert.Equal(4.5m, product.Price);
        }

        [Fact]
        public async Task SoftDeleteAsync_SoftDelete_LeProduit()
        {
            // Seed un produit
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Product (Name, Description, Price, CategoryId, IsOnMenu, ImageBase64, IsDeleted)
                                    VALUES ('Test', 'Desc', 1.99, 1, 1, 'img.png', 0)";
                cmd.ExecuteNonQuery();
            }

            var result = await _service.SoftDeleteAsync(1);
            Assert.True(result);

            // Le produit doit être IsDeleted = 1
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT IsDeleted FROM Product WHERE Id = 1";
                var deleted = Convert.ToInt32(cmd.ExecuteScalar());
                Assert.Equal(1, deleted);
            }
        }

        [Fact]
        public async Task GetAllAsync_RetourneUniquementLesNonSupprimes()
        {
            // Seed deux produits, un supprimé
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Product (Name, Description, Price, CategoryId, IsOnMenu, ImageBase64, IsDeleted)
                                    VALUES ('P1', 'A', 1, 1, 1, 'img.png', 0), ('P2', 'B', 2, 1, 1, 'img.png', 1)";
                cmd.ExecuteNonQuery();
            }

            var products = await _service.GetAllAsync();
            Assert.Single(products);
            Assert.Equal("P1", products[0].Name);

            // Si includeDeleted=true, il doit tous les renvoyer
            var all = await _service.GetAllAsync(includeDeleted: true);
            Assert.Equal(2, all.Count);
        }
    }
}