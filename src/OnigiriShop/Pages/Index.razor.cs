using Microsoft.AspNetCore.Components;
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
        protected Product? ModalProduct;
        protected int ModalQuantity = 1;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _products = await ProductService.GetMenuProductsAsync();
            CartState.OnChanged += OnCartChanged;
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
        }

        private void OnCartChanged() => InvokeAsync(StateHasChanged);

        protected void OpenProductModal(Product product)
        {
            ModalProduct = product;
            ModalQuantity = 1;
        }

        protected int GetQuantity(int productId)
            => CartState.Items.FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;

        protected async Task AddToCart(Product product)
        {
            await CartProvider.AddItemAsync(product.Id, 1);
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
        }

        protected string GetProductImage(Product? p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.ImageBase64))
                return string.Empty;
            return p.ImageBase64.StartsWith("data:image")
                ? p.ImageBase64
                : $"data:image/jpeg;base64,{p.ImageBase64}";
        }

        protected void CloseProductModal() => ModalProduct = null;

        protected void IncreaseQty() => ModalQuantity++;

        protected void DecreaseQty()
        {
            if (ModalQuantity > 1) ModalQuantity--;
        }

        protected async Task AddSelectedToCart()
        {
            if (ModalProduct == null) return;
            await CartProvider.AddItemAsync(ModalProduct.Id, ModalQuantity);
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
            ModalProduct = null;
        }

        public void Dispose() => CartState.OnChanged -= OnCartChanged;
    }
}
