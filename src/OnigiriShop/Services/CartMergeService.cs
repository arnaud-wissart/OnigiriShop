using Microsoft.AspNetCore.Components.Authorization;
using static OnigiriShop.Services.CartProvider;

namespace OnigiriShop.Services;

public class CartMergeService(
    CartService cartService,
    AnonymousCartService anonymousCartService,
    AuthenticationStateProvider authProvider)
{
    private int? _lastMigratedUserId = null;

    public void ResetMigrationFlag() => _lastMigratedUserId = null;

    private async Task<(bool isAuthenticated, int? userId)> GetCurrentUserIdAsync()
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var isAuthenticated = user.Identity?.IsAuthenticated == true;
        int? userId = null;

        if (isAuthenticated)
        {
            var userIdStr = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            userId = int.TryParse(userIdStr, out var tmpId) ? tmpId : null;
        }
        return (isAuthenticated, userId);
    }

    public async Task<CartMergeStatus> DetectCartConflictAsync()
    {
        var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
        if (!isAuthenticated || !userId.HasValue) return CartMergeStatus.None;

        await anonymousCartService.LoadFromLocalStorageAsync();
        var anonItems = anonymousCartService.Items.ToList();
        var sqlItems = await cartService.GetCartItemsWithProductsAsync(userId.Value) ?? [];

        if (anonItems.Count > 0 && sqlItems.Count > 0)
            return CartMergeStatus.BothNonEmpty;
        if (anonItems.Count > 0)
            return CartMergeStatus.AnonymousOnly;
        if (sqlItems.Count > 0)
            return CartMergeStatus.SqlOnly;
        return CartMergeStatus.None;
    }

    public async Task MergeCartsAsync(bool force = false)
    {
        var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
        if (!isAuthenticated || !userId.HasValue) return;

        await anonymousCartService.LoadFromLocalStorageAsync();
        var anonItems = anonymousCartService.Items.ToList();

        if (anonItems.Count == 0) return;

        var sqlItems = await cartService.GetCartItemsWithProductsAsync(userId.Value) ?? [];
        if (!force && sqlItems.Count > 0)
            return;

        foreach (var item in anonItems)
            await cartService.AddItemAsync(userId.Value, item.ProductId, item.Quantity);

        await anonymousCartService.ClearAsync();
        _lastMigratedUserId = userId;
    }

    public async Task ReplaceSqlWithAnonymousAsync()
    {
        var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
        if (!isAuthenticated || !userId.HasValue) return;

        await cartService.ClearCartAsync(userId.Value);

        await anonymousCartService.LoadFromLocalStorageAsync();
        var anonItems = anonymousCartService.Items.ToList();
        foreach (var item in anonItems)
            await cartService.AddItemAsync(userId.Value, item.ProductId, item.Quantity);

        await anonymousCartService.ClearAsync();
        _lastMigratedUserId = userId;
    }

    public async Task MigrateAnonymousCartToUserAsync(bool forceMerge = false)
    {
        var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
        if (!isAuthenticated || !userId.HasValue) return;

        await anonymousCartService.LoadFromLocalStorageAsync();
        var anonItems = anonymousCartService.Items.ToList();

        if (anonItems.Count == 0) return;

        var sqlItems = await cartService.GetCartItemsWithProductsAsync(userId.Value) ?? [];
        if (!forceMerge && sqlItems.Count > 0)
            return;

        if (forceMerge)
            foreach (var anonItem in anonItems)
                await cartService.AddItemAsync(userId.Value, anonItem.ProductId, anonItem.Quantity);
        else
            foreach (var anonItem in anonItems)
                await cartService.AddItemAsync(userId.Value, anonItem.ProductId, anonItem.Quantity);

        await anonymousCartService.ClearAsync();
        _lastMigratedUserId = userId;
    }

    public int? LastMigratedUserId => _lastMigratedUserId;
}
