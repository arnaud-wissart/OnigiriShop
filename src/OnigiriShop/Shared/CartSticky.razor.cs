using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Services;

namespace OnigiriShop.Shared
{
    public partial class CartSticky : ComponentBase, IDisposable
    {
        [Inject] public IJSRuntime JS { get; set; }

        [Inject] public CartService CartService { get; set; }
        [Inject] public AuthService AuthService { get; set; }
        [Inject] public NavigationManager Nav { get; set; }
        [Inject] public CartState CartState { get; set; }

        protected List<CartItemWithProduct> _items = new();
        protected decimal _totalPrice = 0;
        protected bool _hasItems = false;
        [Parameter] public bool ShowCart { get; set; } = true;

        private int _userId;

        protected override async Task OnInitializedAsync()
        {
            var id = await AuthService.GetCurrentUserIdIntAsync();
            if (id.HasValue)
            {
                _userId = id.Value;
                await RefreshCartAsync();
            }
            // S'abonner aux notifs
            CartState.OnChanged += OnCartChanged;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (ShowCart)
                await JS.InvokeVoidAsync("adjustCartContentHeight");


        }
        public async Task RefreshCartAsync()
        {
            if (_userId != 0)
            {
                _items = await CartService.GetCartItemsWithProductsAsync(_userId);
                _hasItems = _items.Count != 0;
                _totalPrice = _items.Sum(x => x.Quantity * x.Product.Price);
                StateHasChanged();

                await JS.InvokeVoidAsync("adjustCartContentHeight");
            }
        }


        private async void OnCartChanged() => await RefreshCartAsync();

        protected async Task IncrementItem(CartItemWithProduct item)
        {
            if (_userId != 0)
            {
                await CartService.AddItemAsync(_userId, item.ProductId, 1);
                CartState.NotifyChanged();
            }
        }

        protected async Task DecrementItem(CartItemWithProduct item)
        {
            if (_userId != 0)
            {
                await CartService.RemoveItemAsync(_userId, item.ProductId, 1);
                CartState.NotifyChanged();
            }
        }
        protected void GoToHome() => Nav.NavigateTo("/");

        public void Dispose() => CartState.OnChanged -= OnCartChanged;
    }
}