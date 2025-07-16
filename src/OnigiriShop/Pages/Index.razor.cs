using Microsoft.AspNetCore.Components;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class IndexBase : CustomComponent, IDisposable
    {
        [Inject] public CartState CartState { get; set; }
        [Inject] public CartService CartService { get; set; }
        [Inject] public ProductService ProductService { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }

        protected List<Product> _products;
        protected HashSet<int> _addedProductIds = [];
        protected bool _showLoginModal;
        protected List<CartItemWithProduct> _cartItems = new();

        protected void ShowLoginModal() => _showLoginModal = true;
        protected void HideLoginModal() => _showLoginModal = false;

        protected override async Task OnInitializedAsync()
        {
            _products = await ProductService.GetMenuProductsAsync();
            CartState.OnChanged += RefreshCartFromEvent;

            CartState.NotifyChanged();

            var userId = await AuthService.GetCurrentUserIdIntAsync();
            if (userId.HasValue)
                _cartItems = await CartService.GetCartItemsWithProductsAsync(userId.Value);
        }
        private async void RefreshCartFromEvent()
        {
            var userId = await AuthService.GetCurrentUserIdIntAsync();
            if (userId.HasValue)
                _cartItems = await CartService.GetCartItemsWithProductsAsync(userId.Value);
            
            await InvokeAsync(StateHasChanged);
        }
        protected int GetProductCartQty(int productId) => _cartItems.FirstOrDefault(x => x.ProductId == productId)?.Quantity ?? 0;
        protected string GetCartIconClass(int qty) => qty > 0 ? "bi bi-cart-check-fill" : "bi bi-cart-plus";

        public async Task TryAddToCart(Product product)
        {
            var userId = await AuthService.GetCurrentUserIdIntAsync();
            if (userId is null)
            {
                ShowLoginModal();
                return;
            }
            await CartService.AddItemAsync(userId.Value, product.Id, 1);

            _cartItems = await CartService.GetCartItemsWithProductsAsync(userId.Value);

            CartState.NotifyChanged();

            _addedProductIds.Add(product.Id);
            StateHasChanged();
            await Task.Delay(200);
            _addedProductIds.Remove(product.Id);
            StateHasChanged();
        }

        public async Task RemoveFromCart(Product product)
        {
            var userId = await AuthService.GetCurrentUserIdIntAsync();
            if (userId is null)
            {
                ShowLoginModal();
                return;
            }
            await CartService.RemoveItemAsync(userId.Value, product.Id, 1);
            CartState.NotifyChanged();
            // Reload cart
            _cartItems = await CartService.GetCartItemsWithProductsAsync(userId.Value);
            StateHasChanged();
        }
        public void Dispose() => CartState.OnChanged -= RefreshCartFromEvent;
    }
}
