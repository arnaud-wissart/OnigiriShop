using Microsoft.AspNetCore.Components;
using OnigiriShop.Infrastructure;
using OnigiriShop.Data.Models;
using Microsoft.JSInterop;

namespace OnigiriShop.Shared;

public class CartBarBase : FrontCustomComponentBase, IDisposable
{
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Parameter] public bool ShowCart { get; set; } = true;

    protected int _itemsCount;
    protected decimal _totalPrice;
    protected bool _hasItems;
    protected bool _showModal;
    protected List<CartItemWithProduct> _items = [];
    protected CartItemWithProduct? ItemToRemove { get; set; }
    protected bool ShowRemoveModal { get; set; }
    protected bool ShowClearCartModal { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        CartState.OnChanged += OnCartChanged;
        await RefreshAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) => await JS.InvokeVoidAsync("activateTooltips");

    private async void OnCartChanged()
    {
        await RefreshAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshAsync()
    {
        _items = await CartProvider.GetCurrentCartItemsWithProductsAsync() ?? [];
        _itemsCount = _items.Sum(i => i.Quantity);
        _totalPrice = _items.Sum(i => i.Quantity * i.Product!.Price);
        _hasItems = _itemsCount > 0;
    }

    protected void OnClick()
    {
        if (ShowCart)
            _showModal = true;
        else
            Nav.NavigateTo("/");
    }

    protected void CloseModal() => _showModal = false;

    protected async Task IncrementQuantity(CartItemWithProduct item)
    {
        await CartProvider.AddItemAsync(item.ProductId, 1);
        CartState.NotifyChanged();
    }

    protected async Task DecrementQuantity(CartItemWithProduct item)
    {
        if (item.Quantity == 1)
            PromptRemoveItem(item);
        else
        {
            await CartProvider.RemoveItemAsync(item.ProductId, 1);
            CartState.NotifyChanged();
        }
    }

    protected void PromptRemoveItem(CartItemWithProduct item)
    {
        ItemToRemove = item;
        ShowRemoveModal = true;
    }

    protected async Task CancelRemove()
    {
        ShowRemoveModal = false;
        ItemToRemove = null;
        await RefreshAsync();
        StateHasChanged();
    }

    protected async Task ConfirmRemove()
    {
        if (ItemToRemove != null)
            await CartProvider.RemoveItemAsync(ItemToRemove.ProductId, ItemToRemove.Quantity);

        ShowRemoveModal = false;
        ItemToRemove = null;
        CartState.NotifyChanged();
    }

    protected void PromptClearCart() => ShowClearCartModal = true;

    protected async Task CancelClearCart()
    {
        ShowClearCartModal = false;
        await RefreshAsync();
        StateHasChanged();
    }

    protected async Task ConfirmClearCart()
    {
        await CartProvider.ClearCartAsync();
        await CartProvider.RefreshCartStateAsync(CartState);
        CartState.NotifyChanged();
        await RefreshAsync();
        StateHasChanged();
        ShowClearCartModal = false;
        CloseModal();
    }

    protected void GoToCart()
    {
        _showModal = false;
        Nav.NavigateTo("/panier");
    }

    public void Dispose() => CartState.OnChanged -= OnCartChanged;
}
