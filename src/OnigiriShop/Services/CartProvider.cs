using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class CartProvider(
        CartService cartService,
        AnonymousCartService anonymousCartService,
        IProductService productService,
        AuthenticationStateProvider authProvider,
        CartMergeService cartMergeService)
    {
        public async Task<List<CartItemWithProduct>> GetCurrentCartItemsWithProductsAsync()
        {
            var (isAuthenticated, userId) = await GetCurrentUserIdAsync();

            if (isAuthenticated && userId.HasValue && cartMergeService.LastMigratedUserId != userId)
            {
                var conflict = await cartMergeService.DetectCartConflictAsync();
                if (conflict == CartMergeStatus.AnonymousOnly)
                    await cartMergeService.MergeCartsAsync(force: true); // Fusion simple
            }

            if (isAuthenticated && userId.HasValue)
            {
                return await cartService.GetCartItemsWithProductsAsync(userId.Value) ?? [];
            }
            else
            {
                var allProducts = await productService.GetMenuProductsAsync();
                return anonymousCartService.Items
                    .Select(ac => new CartItemWithProduct
                    {
                        Id = 0,
                        CartId = 0,
                        ProductId = ac.ProductId,
                        Quantity = ac.Quantity,
                        Product = allProducts.FirstOrDefault(p => p.Id == ac.ProductId)
                    })
                    .Where(x => x.Product != null)
                    .ToList();
            }
        }
        public async Task AddItemAsync(int productId, int quantity)
        {
            var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
            if (isAuthenticated && userId.HasValue)
                await cartService.AddItemAsync(userId.Value, productId, quantity);
            else
                await anonymousCartService.AddItemAsync(productId, quantity);
        }

        public async Task RemoveItemAsync(int productId, int quantity)
        {
            var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
            if (isAuthenticated && userId.HasValue)
                await cartService.RemoveItemAsync(userId.Value, productId, quantity);
            else
                await anonymousCartService.RemoveItemAsync(productId, quantity);
        }

        public async Task ClearCartAsync()
        {
            var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
            if (isAuthenticated && userId.HasValue)
            {
                await cartService.ClearCartAsync(userId.Value);
                await anonymousCartService.ClearAsync();
            }
            else
                await anonymousCartService.ClearAsync();
        }

        public void ResetMigrationFlag() => cartMergeService.ResetMigrationFlag();

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

        public async Task RefreshCartStateAsync(CartState cartState)
        {
            var items = await GetCurrentCartItemsWithProductsAsync();
            cartState.SetItems(items);
        }
        public async Task ReplaceSqlWithAnonymousAsync() => await cartMergeService.ReplaceSqlWithAnonymousAsync();
        public async Task MigrateAnonymousCartToUserAsync() => await cartMergeService.MigrateAnonymousCartToUserAsync();
        public async Task<CartMergeStatus> DetectCartConflictAsync() => await cartMergeService.DetectCartConflictAsync();

        public enum CartMergeStatus
        {
            None,           // Aucun panier
            AnonymousOnly,  // Panier anonyme uniquement
            SqlOnly,        // Panier SQL uniquement
            BothNonEmpty    // Conflit à gérer, les deux paniers sont non vides
        }
    }
}
