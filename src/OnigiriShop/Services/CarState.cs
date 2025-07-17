using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class CartState
    {
        public event Action OnChanged;

        private List<CartItemWithProduct> _items = new();
        public IReadOnlyList<CartItemWithProduct> Items => _items;

        public void SetItems(List<CartItemWithProduct> items)
        {
            _items = items ?? new();
            OnChanged?.Invoke();
        }

        public void NotifyChanged() => OnChanged?.Invoke();
    }
}
