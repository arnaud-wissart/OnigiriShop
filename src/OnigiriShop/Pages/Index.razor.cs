using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class IndexBase : FrontCustomComponentBase, IDisposable
    {
        [Inject] public ProductService ProductService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;

        protected List<Product>? _products;
        protected HashSet<int> _addedProductIds = [];

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _products = await ProductService.GetMenuProductsAsync();
            CartState.OnChanged += OnCartChanged;
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
        }

        private void OnCartChanged() => InvokeAsync(StateHasChanged);

        public async Task TryAddToCart(Product product)
        {
            await CartProvider.AddItemAsync(product.Id, 1);
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();

            _addedProductIds.Add(product.Id);
            StateHasChanged();
            await Task.Delay(200);
            _addedProductIds.Remove(product.Id);
            StateHasChanged();
        }
        public async Task OnRemoveFromCartClicked(Product product)
        {
            await CartProvider.RemoveItemAsync(product.Id, 1);
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();

            StateHasChanged();
        }


        public async Task OnAddToCartClicked(Product product)
        {
            await HandleAsync(
                async () => await TryAddToCart(product),
                "Erreur lors de l’ajout au panier.",
                null,
                true
            );
            await JS.InvokeVoidAsync("closeAllTooltips");
        }

        protected string GetCartIconClass(int productId)
        {
            var qty = GetProductCartQty(productId);
            return qty > 0 ? "bi bi-cart-check-fill" : "bi bi-cart-plus";
        }

        protected int GetProductCartQty(int productId)
            => CartState.Items.FirstOrDefault(x => x.ProductId == productId)?.Quantity ?? 0;

        public void Dispose() => CartState.OnChanged -= OnCartChanged;
    }
}