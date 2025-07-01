using Microsoft.AspNetCore.Components;
using OnigiriShop.Services;

namespace OnigiriShop.Shared
{
    public partial class CartSticky : ComponentBase, IDisposable
    {
        [Inject] public CartService CartService { get; set; }

        protected List<CartItem> _items = [];
        protected decimal _totalPrice = 0;
        protected bool _hasItems = false;

        protected override void OnInitialized()
        {
            RefreshCart();
            CartService.CartChanged += OnCartChanged;
        }
        private void OnCartChanged()
        {
            InvokeAsync(RefreshCart); // thread-safe pour Blazor
        }

        protected void RemoveItem(int productId)
        {
            CartService.Remove(productId);
            RefreshCart();
        }
        public void Dispose() => CartService.CartChanged -= OnCartChanged;
        protected void RefreshCart()
        {
            _items = CartService.Items.ToList();
            _hasItems = _items.Count != 0;
            _totalPrice = _items.Sum(x => x.Product.Price * x.Quantity);
            StateHasChanged();
        }
    }
}
