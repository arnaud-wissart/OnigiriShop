using OnigiriShop.Data.Models;
using System.Text.Json;

namespace OnigiriShop.Data
{
    public class OrderManager
    {
        private readonly string _dataFolder;

        public OrderManager(string dataFolder)
        {
            _dataFolder = dataFolder;
        }

        private string GetOrderFileName(DateTime date)
            => Path.Combine(_dataFolder, $"orders_{date:yyyy_MM}.json");

        public async Task<List<Order>> LoadOrdersAsync(DateTime month)
        {
            var filePath = GetOrderFileName(month);
            if (!File.Exists(filePath))
                return new List<Order>();
            using var stream = File.OpenRead(filePath);
            var orders = await JsonSerializer.DeserializeAsync<List<Order>>(stream);
            return orders ?? new List<Order>();
        }

        public async Task AddOrderAsync(Order order)
        {
            var filePath = GetOrderFileName(order.OrderDate);
            var orders = new List<Order>();
            if (File.Exists(filePath))
            {
                using var read = File.OpenRead(filePath);
                orders = await JsonSerializer.DeserializeAsync<List<Order>>(read) ?? new List<Order>();
            }
            orders.Add(order);
            using var write = File.Create(filePath);
            await JsonSerializer.SerializeAsync(write, orders, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task SaveOrdersAsync(DateTime month, List<Order> orders)
        {
            var filePath = GetOrderFileName(month);
            using var write = File.Create(filePath);
            await JsonSerializer.SerializeAsync(write, orders, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
