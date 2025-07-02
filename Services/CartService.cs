using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class CartService
    {
        private List<CartItem> _items = new();
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
            }
        }

        public int GetProductCount(int productId)
        {
            var item = _items.FirstOrDefault(i => i.Product.Id == productId);
            return item?.Quantity ?? 0;
        }

        public void Clear()
        {
            _items.Clear();
            CartChanged?.Invoke();
        }

        public int TotalCount() => _items.Sum(i => i.Quantity);
        public decimal TotalPrice() => _items.Sum(i => i.Quantity * i.Product.Price);
        public bool HasItems() => _items.Any();
    }


    public class CartItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }
}