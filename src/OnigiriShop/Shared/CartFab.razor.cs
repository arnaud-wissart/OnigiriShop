using Microsoft.AspNetCore.Components;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Shared;

public class CartFabBase : FrontCustomComponentBase, IDisposable
{
    [Parameter] public bool ShowCart { get; set; } = true;
    protected int _itemsCount;
    protected bool _hasItems;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        CartState.OnChanged += OnCartChanged;
        await CartProvider.RefreshCartStateAsync(CartState);
        UpdateState();
    }

    private void OnCartChanged()
    {
        UpdateState();
        InvokeAsync(StateHasChanged);
    }

    private void UpdateState()
    {
        _itemsCount = CartState.Items.Sum(i => i.Quantity);
        _hasItems = _itemsCount > 0;
    }

    public void Dispose() => CartState.OnChanged -= OnCartChanged;
}
