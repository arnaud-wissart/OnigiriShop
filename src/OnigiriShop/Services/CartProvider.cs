using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class CartProvider(
        CartService cartService,
        AnonymousCartService anonymousCartService,
        ProductService productService,
        AuthenticationStateProvider authProvider)
    {
        private int? _lastMigratedUserId = null;

        public async Task<List<CartItemWithProduct>> GetCurrentCartItemsWithProductsAsync()
        {
            var (isAuthenticated, userId) = await GetCurrentUserIdAsync();

            // Migration automatique uniquement si pas déjà migré, et pas de conflit (voir DetectCartConflictAsync pour modale)
            if (isAuthenticated && userId.HasValue && _lastMigratedUserId != userId)
            {
                // On ne migre QUE si pas de conflit
                var conflict = await DetectCartConflictAsync();
                if (conflict == CartMergeStatus.AnonymousOnly)
                {
                    await MergeCartsAsync(force: true); // Fusion simple
                    _lastMigratedUserId = userId;
                }
                // Si BothNonEmpty, on attend la décision de l’utilisateur (modale)
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

        /// <summary>
        /// Fusionne les deux paniers (anonyme et SQL), utilisé si l'utilisateur choisit "Fusionner"
        /// </summary>
        public async Task MergeCartsAsync(bool force = false)
        {
            var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
            if (!isAuthenticated || !userId.HasValue) return;

            await anonymousCartService.LoadFromLocalStorageAsync();
            var anonItems = anonymousCartService.Items.ToList();

            if (anonItems.Count == 0) return;

            // Si pas de force, vérifie qu'il n'y a pas déjà un panier SQL avec des articles
            var sqlItems = await cartService.GetCartItemsWithProductsAsync(userId.Value) ?? [];
            if (!force && sqlItems.Count > 0)
                return; // Ne fusionne pas si les deux sont non vides, il faut un choix utilisateur

            foreach (var item in anonItems)
                await cartService.AddItemAsync(userId.Value, item.ProductId, item.Quantity);

            await anonymousCartService.ClearAsync();
            _lastMigratedUserId = userId;
        }

        /// <summary>
        /// Remplace le panier SQL par l’anonyme (utilisé si l'utilisateur choisit "Remplacer")
        /// </summary>
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
                await cartService.ClearCartAsync(userId.Value);
            else
                await anonymousCartService.ClearAsync();
        }

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
        public async Task MigrateAnonymousCartToUserAsync(bool forceMerge = false)
        {
            var (isAuthenticated, userId) = await GetCurrentUserIdAsync();
            if (!isAuthenticated || !userId.HasValue) return;

            await anonymousCartService.LoadFromLocalStorageAsync();
            var anonItems = anonymousCartService.Items.ToList();

            if (anonItems.Count == 0) return;

            var sqlItems = await cartService.GetCartItemsWithProductsAsync(userId.Value) ?? [];
            if (!forceMerge && sqlItems.Count > 0)
                return; // Si on ne force pas et qu'il y a déjà un panier SQL, ne rien faire

            if (forceMerge)
            {
                // FUSION : ajoute les articles anonymes sans supprimer ceux du SQL (ajoute quantité à quantité si doublons)
                foreach (var anonItem in anonItems)
                {
                    // Recherche si le produit existe déjà dans le panier SQL
                    var existingSqlItem = sqlItems.FirstOrDefault(x => x.ProductId == anonItem.ProductId);
                    if (existingSqlItem != null)
                    {
                        // Ajoute la quantité au SQL
                        await cartService.AddItemAsync(userId.Value, anonItem.ProductId, anonItem.Quantity);
                    }
                    else
                    {
                        await cartService.AddItemAsync(userId.Value, anonItem.ProductId, anonItem.Quantity);
                    }
                }
            }
            else
            {
                // MIGRATION SIMPLE : panier SQL doit être vide
                foreach (var anonItem in anonItems)
                {
                    await cartService.AddItemAsync(userId.Value, anonItem.ProductId, anonItem.Quantity);
                }
            }

            await anonymousCartService.ClearAsync();
            _lastMigratedUserId = userId;
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

        public async Task RefreshCartStateAsync(CartState cartState)
        {
            var items = await GetCurrentCartItemsWithProductsAsync();
            cartState.SetItems(items);
        }


        public enum CartMergeStatus
        {
            None,           // Aucun panier
            AnonymousOnly,  // Panier anonyme uniquement
            SqlOnly,        // Panier SQL uniquement
            BothNonEmpty    // Conflit à gérer, les deux paniers sont non vides
        }

    }
}
