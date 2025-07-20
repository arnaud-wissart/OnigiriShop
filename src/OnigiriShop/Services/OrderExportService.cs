using OnigiriShop.Data.Models;
using System.Text;

namespace OnigiriShop.Services
{
    public class OrderExportService(OrderService orderService)
    {
        private readonly OrderService _orderService = orderService;

        public async Task<string> BuildExportTextAsync(IEnumerable<AdminOrderSummary> orders)
        {
            var sb = new StringBuilder();
            foreach (var order in orders.OrderBy(o => o.DeliveryAt))
            {
                var items = await _orderService.GetOrderItemsAsync(order.Id);
                sb.AppendLine($"Livraison le {order.DeliveryAt:dddd dd/MM/yyyy HH:mm} à {order.DeliveryPlace}");
                sb.AppendLine(order.UserDisplayName);
                sb.AppendLine();

                foreach (var item in items)
                    sb.AppendLine($"  {item.Quantity} x {item.ProductName}");

                sb.AppendLine();
                sb.AppendLine($"Total : {order.TotalAmount:0.00} €");
                sb.AppendLine(new string('-', 32));
            }
            return sb.ToString();
        }
    }
}