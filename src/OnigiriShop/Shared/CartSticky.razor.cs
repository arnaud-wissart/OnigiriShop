using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using static OnigiriShop.Services.CartProvider;

namespace OnigiriShop.Shared
{
    public partial class CartStickyBase : FrontCustomComponentBase, IDisposable
    {
        [Inject] public NavigationManager Nav { get; set; }
        [Parameter] public EventCallback OnCartUpdated { get; set; }
        [Parameter] public bool ShowCart { get; set; } = true;

        protected List<CartItemWithProduct> _items = new();
        protected decimal _totalPrice = 0;
        protected bool _hasItems = false;
        protected CartMergeStatus? _cartConflictStatus = null;
        protected bool _showCartMergeModal = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await RefreshCartAsync();
            CartState.OnChanged += OnCartChanged;

            var status = await CartProvider.DetectCartConflictAsync();
            if (status == CartMergeStatus.BothNonEmpty)
            {
                _cartConflictStatus = status;
                _showCartMergeModal = true;
                StateHasChanged();
            }
        }
        protected async Task MergeCarts()
        {
            await CartProvider.MigrateAnonymousCartToUserAsync(forceMerge: true);
            _showCartMergeModal = false;
            CartState.NotifyChanged();
            await RefreshCartAsync();
        }

        protected async Task ReplaceSqlWithAnon()
        {
            await CartProvider.ClearCartAsync();
            await CartProvider.MigrateAnonymousCartToUserAsync(forceMerge: true);
            _showCartMergeModal = false;
            CartState.NotifyChanged();
            await RefreshCartAsync();
        }

        protected void CancelCartMerge()
        {
            _showCartMergeModal = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (ShowCart)
                await JS.InvokeVoidAsync("adjustCartContentHeight");
        }

        public async Task RefreshCartAsync()
        {
            _items = await CartProvider.GetCurrentCartItemsWithProductsAsync() ?? [];
            _hasItems = _items.Count != 0;
            _totalPrice = _items.Sum(x => x.Quantity * x.Product.Price);

            StateHasChanged();
            await JS.InvokeVoidAsync("adjustCartContentHeight");
        }

        private bool _refreshing;

        private async void OnCartChanged()
        {
            if (_refreshing) return;
            _refreshing = true;
            await RefreshCartAsync();
            _refreshing = false;
        }

        protected async Task IncrementItem(CartItemWithProduct item)
        {
            await CartProvider.AddItemAsync(item.ProductId, 1);
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
        }

        protected async Task DecrementItem(CartItemWithProduct item)
        {
            await CartProvider.RemoveItemAsync(item.ProductId, 1);
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
        }

        protected void GoToHome() => Nav.NavigateTo("/");

        public void Dispose() => CartState.OnChanged -= OnCartChanged;
    }
}
