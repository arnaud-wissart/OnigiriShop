using Microsoft.JSInterop;
using OnigiriShop.Data.Models;
using System.Text.Json;

namespace OnigiriShop.Services
{
    public class AnonymousCartService(IJSRuntime js)
    {
        private List<CartItem> _items = new();
        public IReadOnlyList<CartItem> Items => _items;

        public async Task AddItemAsync(int productId, int quantity)
        {
            var item = _items.FirstOrDefault(x => x.ProductId == productId);
            if (item == null)
                _items.Add(new CartItem { ProductId = productId, Quantity = quantity });
            else
                item.Quantity += quantity;

            await SaveToLocalStorageAsync();
        }

        public async Task RemoveItemAsync(int productId, int quantity)
        {
            var item = _items.FirstOrDefault(x => x.ProductId == productId);
            if (item == null) return;
            if (item.Quantity <= quantity)
                _items.Remove(item);
            else
                item.Quantity -= quantity;

            await SaveToLocalStorageAsync();
        }

        public async Task ClearAsync()
        {
            _items.Clear();
            await SaveToLocalStorageAsync();
        }

        public async Task SaveToLocalStorageAsync()
        {
            var json = JsonSerializer.Serialize(_items);
            await js.InvokeVoidAsync("localStorage.setItem", "anonCart", json);
        }

        public async Task LoadFromLocalStorageAsync()
        {
            var json = await js.InvokeAsync<string>("localStorage.getItem", "anonCart");
            if (!string.IsNullOrWhiteSpace(json))
            {
                var newItems = JsonSerializer.Deserialize<List<CartItem>>(json);
                _items = newItems ?? new();
            }
        }

        public async Task ClearLocalStorageAsync()
            => await js.InvokeVoidAsync("localStorage.removeItem", "anonCart");
    }
}
