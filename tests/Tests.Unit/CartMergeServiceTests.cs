using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Moq;
using OnigiriShop.Services;

namespace Tests.Unit;

public class CartMergeServiceTests
{
    private static (CartMergeService service, AnonymousCartService anonCart) BuildService(FakeJsRuntime js, CartService cartService)
    {
        var anonCart = new AnonymousCartService(js);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "test"));
        var authProvider = new Mock<AuthenticationStateProvider>();
        authProvider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(user));
        var service = new CartMergeService(cartService, anonCart, authProvider.Object);
        return (service, anonCart);
    }

    [Fact]
    public async Task DetectCartConflictAsync_Returns_BothNonEmpty()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var cartService = new CartService(new FakeSqliteConnectionFactory(conn));
        var js = new FakeJsRuntime();
        var (service, anonCart) = BuildService(js, cartService);

        await cartService.AddItemAsync(1, 1, 1, conn);
        await anonCart.AddItemAsync(2, 1);

        var status = await service.DetectCartConflictAsync();
        Assert.Equal(CartProvider.CartMergeStatus.BothNonEmpty, status);
    }

    [Fact]
    public async Task MergeCartsAsync_Merges_WhenSqlEmpty()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var cartService = new CartService(new FakeSqliteConnectionFactory(conn));
        var js = new FakeJsRuntime();
        var (service, anonCart) = BuildService(js, cartService);

        await anonCart.AddItemAsync(1, 2);

        await service.MergeCartsAsync();

        var items = await cartService.GetCartItemsAsync(1, conn);
        Assert.Single(items);
        Assert.Equal(2, items[0].Quantity);
        Assert.Empty(anonCart.Items);
    }

    [Fact]
    public async Task MergeCartsAsync_NoMerge_IfSqlNotEmpty_WithoutForce()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var cartService = new CartService(new FakeSqliteConnectionFactory(conn));
        var js = new FakeJsRuntime();
        var (service, anonCart) = BuildService(js, cartService);

        await cartService.AddItemAsync(1, 2, 1, conn);
        await anonCart.AddItemAsync(2, 1);

        await service.MergeCartsAsync();

        var sqlItems = await cartService.GetCartItemsAsync(1, conn);
        Assert.Single(sqlItems);
        Assert.Equal(2, sqlItems[0].ProductId);
        Assert.Single(anonCart.Items); // still there
    }
}
