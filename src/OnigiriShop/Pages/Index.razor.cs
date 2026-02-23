using Microsoft.AspNetCore.Components;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;

namespace OnigiriShop.Pages
{
    public class IndexBase : FrontCustomComponentBase, IDisposable
    {
        [Inject] public IProductService ProductService { get; set; } = default!;
        [Inject] public ICategoryService CategoryService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;

        protected List<Product>? _products;
        protected List<Category> Categories { get; set; } = [];
        protected Product? ModalProduct;
        protected int ModalQuantity = 1;

        private static readonly string GenericOnigiriImage = CreateOnigiriSvgDataUri("ONIGIRI", "#f48b6a", "#ffe8de");
        private static readonly string GenericSpongeImage = CreateSpongeCakeSvgDataUri("SPONGE", "#8d6e63", "#f6eee7");
        private static readonly string GenericProductImage = CreateFallbackSvgDataUri("ONIGIRI SHOP");

        private static readonly IReadOnlyDictionary<string, string> ProductImageOverrides =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Onigiri Saumon"] = CreateOnigiriSvgDataUri("SAUMON", "#ff8a65", "#ffe7df"),
                ["Onigiri Thon Mayo"] = CreateOnigiriSvgDataUri("THON MAYO", "#f2d6a2", "#fff0d9"),
                ["Onigiri Umeboshi"] = CreateOnigiriSvgDataUri("UMEBOSHI", "#d84343", "#ffe3e3"),
                ["Onigiri Algues"] = CreateOnigiriSvgDataUri("ALGUES", "#2f7d55", "#e2f3e8"),
                ["Onigiri Poulet Teriyaki"] = CreateOnigiriSvgDataUri("TERIYAKI", "#8d6e63", "#efe4df"),
                ["Onigiri Vegan"] = CreateOnigiriSvgDataUri("VEGAN", "#5aa35a", "#e6f5e6"),
                ["Onigiri Boeuf"] = CreateOnigiriSvgDataUri("BOEUF", "#6d4c41", "#efe3dd"),
                ["Onigiri Tempura Crevette"] = CreateOnigiriSvgDataUri("TEMPURA", "#ffb74d", "#fff1df"),
                ["Onigiri Ebi Mayo"] = CreateOnigiriSvgDataUri("EBI MAYO", "#f29a84", "#ffe9e2"),
                ["Sponge Cake Matcha"] = CreateSpongeCakeSvgDataUri("MATCHA", "#7cad49", "#e7f4d3"),
                ["Sponge Cake Chocolat"] = CreateSpongeCakeSvgDataUri("CHOCOLAT", "#6d4c41", "#f1e4d6")
            };

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _products = await ProductService.GetMenuProductsAsync();
            Categories = await CategoryService.GetAllAsync();
            CartState.OnChanged += OnCartChanged;
            await CartProvider.RefreshCartStateAsync(CartState);
            CartState.NotifyChanged();
        }

        protected void OnCartChanged() => InvokeAsync(StateHasChanged);

        protected void OpenProductModal(Product product)
        {
            ModalProduct = product;
            var qty = GetQuantity(product.Id);
            ModalQuantity = qty > 0 ? qty : 1;
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
            if (p == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(p.Name) && ProductImageOverrides.TryGetValue(p.Name, out var overrideImage))
                return overrideImage;

            if (IsSpongeCake(p))
                return GenericSpongeImage;

            if (IsOnigiri(p))
                return GenericOnigiriImage;

            if (string.IsNullOrWhiteSpace(p.ImageBase64))
                return GenericProductImage;

            return p.ImageBase64.StartsWith("data:image")
                ? p.ImageBase64
                : $"data:image/jpeg;base64,{p.ImageBase64}";
        }

        private static bool IsSpongeCake(Product product)
        {
            var name = product.Name ?? string.Empty;
            var description = product.Description ?? string.Empty;

            return product.CategoryId == 2
                || name.Contains("sponge", StringComparison.OrdinalIgnoreCase)
                || description.Contains("sponge", StringComparison.OrdinalIgnoreCase)
                || description.Contains("matcha", StringComparison.OrdinalIgnoreCase)
                || description.Contains("chocolat", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOnigiri(Product product)
        {
            var name = product.Name ?? string.Empty;
            var description = product.Description ?? string.Empty;

            return product.CategoryId == 1
                || name.Contains("onigiri", StringComparison.OrdinalIgnoreCase)
                || description.Contains("onigiri", StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateOnigiriSvgDataUri(string label, string fillingColor, string backgroundAccent)
        {
            var svg = $$"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 900" role="img" aria-label="{{label}}">
                  <defs>
                    <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
                      <stop offset="0%" stop-color="#fffaf4"/>
                      <stop offset="100%" stop-color="{{backgroundAccent}}"/>
                    </linearGradient>
                  </defs>
                  <rect width="900" height="900" fill="url(#bg)"/>
                  <circle cx="450" cy="430" r="275" fill="#ffffff"/>
                  <path d="M450 145 L735 635 Q748 660 722 682 Q706 695 680 695 H220 Q194 695 178 682 Q152 660 165 635 Z"
                        fill="#fffdf8" stroke="#f0e7db" stroke-width="18"/>
                  <path d="M320 560 H580 A26 26 0 0 1 606 586 V710 H294 V586 A26 26 0 0 1 320 560 Z"
                        fill="#24352f"/>
                  <circle cx="450" cy="470" r="72" fill="{{fillingColor}}"/>
                  <ellipse cx="450" cy="450" rx="26" ry="16" fill="#ffffff" fill-opacity="0.35"/>
                  <text x="450" y="815" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif"
                        font-size="64" font-weight="700" letter-spacing="3" fill="#3a312a">{{label}}</text>
                </svg>
                """;

            return ToSvgDataUri(svg);
        }

        private static string CreateSpongeCakeSvgDataUri(string label, string cakeColor, string creamColor)
        {
            var svg = $$"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 900" role="img" aria-label="{{label}}">
                  <defs>
                    <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
                      <stop offset="0%" stop-color="#fffaf2"/>
                      <stop offset="100%" stop-color="#f5efe6"/>
                    </linearGradient>
                  </defs>
                  <rect width="900" height="900" fill="url(#bg)"/>
                  <ellipse cx="450" cy="640" rx="270" ry="70" fill="#e9ddcf"/>
                  <rect x="230" y="360" width="440" height="95" rx="24" fill="{{cakeColor}}"/>
                  <rect x="230" y="455" width="440" height="42" rx="18" fill="{{creamColor}}"/>
                  <rect x="230" y="497" width="440" height="95" rx="24" fill="{{cakeColor}}"/>
                  <rect x="300" y="330" width="300" height="40" rx="20" fill="#fffdf8"/>
                  <circle cx="335" cy="325" r="10" fill="#d8b48a"/>
                  <circle cx="390" cy="320" r="10" fill="#d8b48a"/>
                  <circle cx="445" cy="323" r="10" fill="#d8b48a"/>
                  <circle cx="500" cy="320" r="10" fill="#d8b48a"/>
                  <circle cx="555" cy="325" r="10" fill="#d8b48a"/>
                  <text x="450" y="815" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif"
                        font-size="64" font-weight="700" letter-spacing="3" fill="#4a3b2f">{{label}}</text>
                </svg>
                """;

            return ToSvgDataUri(svg);
        }

        private static string CreateFallbackSvgDataUri(string label)
        {
            var svg = $$"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 900" role="img" aria-label="{{label}}">
                  <defs>
                    <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
                      <stop offset="0%" stop-color="#fff9f2"/>
                      <stop offset="100%" stop-color="#eee5d8"/>
                    </linearGradient>
                  </defs>
                  <rect width="900" height="900" fill="url(#bg)"/>
                  <circle cx="450" cy="390" r="180" fill="#ffffff" stroke="#eadfce" stroke-width="18"/>
                  <path d="M360 470 H540 A24 24 0 0 1 564 494 V610 H336 V494 A24 24 0 0 1 360 470 Z"
                        fill="#2d3b37"/>
                  <text x="450" y="790" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif"
                        font-size="58" font-weight="700" letter-spacing="3" fill="#453a2f">{{label}}</text>
                </svg>
                """;

            return ToSvgDataUri(svg);
        }

        private static string ToSvgDataUri(string svg)
        {
            return $"data:image/svg+xml;utf8,{Uri.EscapeDataString(svg)}";
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
