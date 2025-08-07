using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using OnigiriShop.Data.Models;
using OnigiriShop.Services;
using System.Security.Claims;

namespace Tests.Unit
{
    public class CartProviderTests
    {
        [Fact]
        public async Task ClearCartAsync_Supprime_Panier_Local_Et_Sql()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();

            var cartService = new CartService(new FakeSqliteConnectionFactory(conn));
            var js = new FakeJsRuntime();
            var anonCart = new AnonymousCartService(js);

            // Ajout d'un article dans chaque panier
            await cartService.AddItemAsync(1, 1, 1, conn);
            await anonCart.AddItemAsync(2, 1);

            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "test"));
            var authProvider = new Mock<AuthenticationStateProvider>();
            authProvider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(user));

            var productService = new Mock<IProductService>();
            var cartMerge = new CartMergeService(cartService, anonCart, authProvider.Object);
            var provider = new CartProvider(cartService, anonCart, productService.Object, authProvider.Object, cartMerge);

            await provider.ClearCartAsync();

            var sqlItems = await cartService.GetCartItemsAsync(1, conn);
            Assert.Empty(sqlItems);
            Assert.Empty(anonCart.Items);
        }
    }
}
