using Microsoft.AspNetCore.Components;
using OnigiriShop.Infrastructure;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Shared;

public class CartBarBase : FrontCustomComponentBase, IDisposable
{
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Parameter] public bool ShowCart { get; set; } = true;

    protected int _itemsCount;
    protected decimal _totalPrice;
    protected bool _hasItems;
    protected bool _showModal;
    protected List<CartItemWithProduct> _items = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        CartState.OnChanged += OnCartChanged;
        await RefreshAsync();
    }

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

    protected void GoToCart() => Nav.NavigateTo("/panier");

    public void Dispose() => CartState.OnChanged -= OnCartChanged;
}
