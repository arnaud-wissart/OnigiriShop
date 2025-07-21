using OnigiriShop.Services;

namespace Tests.Unit
{
    public class CartServiceSqlTests
    {
        [Fact]
        public async Task AddItemAsync_Ajoute_Produit()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();

            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));

            await cartService.AddItemAsync(1, 1, 2, conn);

            var cart = await cartService.GetActiveCartAsync(1, conn);
            Assert.NotNull(cart);
            Assert.NotNull(cart.Items);
            Assert.Single(cart.Items!);
            Assert.Equal(2, cart.Items!.First().Quantity);
            Assert.Equal(1, cart.Items!.First().ProductId);
        }

        [Fact]
        public async Task RemoveItemAsync_Decremente_Produit_Ou_Supprime()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();

            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));

            await cartService.AddItemAsync(1, 1, 2, conn);
            await cartService.RemoveItemAsync(1, 1, 1, conn);

            var cart = await cartService.GetActiveCartAsync(1, conn);
            Assert.NotNull(cart);
            Assert.NotNull(cart.Items);
            Assert.Single(cart.Items!);
            Assert.Equal(1, cart.Items!.First().Quantity);

            await cartService.RemoveItemAsync(1, 1, 1, conn);
            cart = await cartService.GetActiveCartAsync(1, conn);
            Assert.NotNull(cart);
            Assert.NotNull(cart.Items);
            Assert.Empty(cart.Items!);
        }

        [Fact]
        public async Task ClearCartAsync_Vide_Le_Panier()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();

            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));

            await cartService.AddItemAsync(1, 1, 2, conn);
            await cartService.ClearCartAsync(1, conn);

            var cart = await cartService.GetActiveCartAsync(1, conn);
            Assert.NotNull(cart);
            Assert.NotNull(cart.Items);
            Assert.Empty(cart.Items!);
        }

        [Fact]
        public async Task GetActiveCartAsync_AucunPanier_RetourneNull()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();

            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));
            var cart = await cartService.GetActiveCartAsync(1, conn);
            Assert.Null(cart);
        }

        [Fact]
        public async Task CreateOrGetActiveCartAsync_CreeEtRetrouveLePanier()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();

            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));

            var cart1 = await cartService.CreateOrGetActiveCartAsync(1, conn);
            Assert.NotNull(cart1);
            Assert.True(cart1.IsActive);

            var cart2 = await cartService.CreateOrGetActiveCartAsync(1, conn);
            Assert.NotNull(cart2);
            Assert.Equal(cart1.Id, cart2.Id); // Doit retourner le même panier actif
        }

        [Fact]
        public async Task AddItemAsync_AjouteDeuxProduitsDifferents()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();

            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));
            await cartService.AddItemAsync(1, 1, 2, conn);
            await cartService.AddItemAsync(1, 2, 1, conn);

            var cart = await cartService.GetActiveCartAsync(1, conn);
            Assert.NotNull(cart);
            Assert.NotNull(cart.Items);
            Assert.Equal(2, cart.Items!.Count);
            Assert.Contains(cart.Items!, x => x.ProductId == 1 && x.Quantity == 2);
            Assert.Contains(cart.Items!, x => x.ProductId == 2 && x.Quantity == 1);
        }

        [Fact]
        public async Task AddItemAsync_PaniersSeparesPourUtilisateursDifferents()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
            
            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));

            await cartService.AddItemAsync(1, 1, 2, conn);
            await cartService.AddItemAsync(2, 2, 1, conn);

            var cart1 = await cartService.GetActiveCartAsync(1, conn);
            var cart2 = await cartService.GetActiveCartAsync(2, conn);

            Assert.NotNull(cart1);
            Assert.Single(cart1.Items);
            Assert.Equal(1, cart1.Items.First().ProductId);

            Assert.NotNull(cart2);
            Assert.Single(cart2.Items);
            Assert.Equal(2, cart2.Items.First().ProductId);
        }
    }
}