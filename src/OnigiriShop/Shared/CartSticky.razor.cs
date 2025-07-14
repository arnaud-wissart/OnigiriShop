using Microsoft.AspNetCore.Components;
using OnigiriShop.Services;
using static OnigiriShop.Services.CartService;

namespace OnigiriShop.Shared
{
    public partial class CartSticky : ComponentBase, IDisposable
    {
        [Inject] public CartService CartService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        protected List<CartItem> _items = [];
        protected decimal _totalPrice = 0;
        protected bool _hasItems = false;
        [Parameter] public bool ShowCart { get; set; } = true;

        protected override void OnInitialized()
        {
            RefreshCart();
            CartService.CartChanged += OnCartChanged;
        }

        private void OnCartChanged()
        {
            InvokeAsync(RefreshCart); // thread-safe pour Blazor
        }

        protected void IncrementItem(CartItem item)
        {
            CartService.Add(item.Product, 1);
            // Pas besoin de StateHasChanged, il est appelé via RefreshCart (CartChanged event)
        }

        protected void DecrementItem(CartItem item)
        {
            if (item.Quantity <= 1)
                CartService.Remove(item.Product.Id, 1);
            else
                CartService.Remove(item.Product.Id, 1);
        }

        public void Dispose() => CartService.CartChanged -= OnCartChanged;

        protected void RefreshCart()
        {
            _items = CartService.Items.ToList();
            _hasItems = _items.Count != 0;
            _totalPrice = _items.Sum(x => x.Product.Price * x.Quantity);
            StateHasChanged();
        }

        protected void GoToHome() => Nav.NavigateTo("/");
    }
}
