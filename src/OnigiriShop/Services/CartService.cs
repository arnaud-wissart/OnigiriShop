using Microsoft.JSInterop;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class CartService(IJSRuntime js)
    {
        private List<CartItem> _items = [];

        public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
        public event Action CartChanged;

        public void Add(Product product, int quantity = 1)
        {
            var item = _items.Find(i => i.Product.Id == product.Id);
            if (item != null)
                item.Quantity += quantity;
            else
                _items.Add(new CartItem { Product = product, Quantity = quantity });

            CartChanged?.Invoke();
            _ = SaveToLocalStorageAsync();
        }
        public void Remove(int productId, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.Product.Id == productId);
            if (item != null)
            {
                item.Quantity -= quantity;
                if (item.Quantity <= 0)
                    _items.Remove(item);
                CartChanged?.Invoke();
                _ = SaveToLocalStorageAsync();
            }
        }
        public void Clear()
        {
            _items.Clear();
            CartChanged?.Invoke();
            _ = SaveToLocalStorageAsync();
        }
        public async Task InitializeAsync(Func<int, Product> productResolver)
        {
            if (_items.Count == 0) // pour éviter double chargement
                await LoadFromLocalStorageAsync(productResolver);
        }
        public async Task LoadFromLocalStorageAsync(Func<int, Product> productResolver)
        {
            var loaded = await js.InvokeAsync<List<CartJsItem>>("onigiriCart.load");
            if (loaded != null && loaded.Any())
            {
                _items = loaded
                    .Select(i => new CartItem
                    {
                        Product = productResolver(i.ProductId),
                        Quantity = i.Quantity
                    })
                    .Where(i => i.Product != null)
                    .ToList();
                CartChanged?.Invoke();
            }
        }

        private async Task SaveToLocalStorageAsync()
        {
            var serializableCart = _items.Select(i => new
            {
                ProductId = i.Product.Id,
                i.Quantity
            }).ToList();

            await js.InvokeVoidAsync("onigiriCart.save", serializableCart);
        }

        public int GetProductCount(int productId) => _items.FirstOrDefault(i => i.Product.Id == productId)?.Quantity ?? 0;
        public int TotalCount() => _items.Sum(i => i.Quantity);
        public decimal TotalPrice() => _items.Sum(i => i.Quantity * i.Product.Price);
        public bool HasItems() => _items.Any();

        public class CartItem
        {
            public Product Product { get; set; }
            public int Quantity { get; set; }
        }
        public class CartJsItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}