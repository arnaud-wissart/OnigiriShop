namespace OnigiriShop.Data.Models
{
    public class AdminOrderSummary
    {
        public int Id { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty; 
        public DateTime OrderedAt { get; set; }
        public string DeliveryPlace { get; set; } = string.Empty;
        public DateTime DeliveryAt { get; set; }
    }
    public class AdminOrderDetail : AdminOrderSummary
    {
        public List<OrderItem> Items { get; set; } = new();
        public static AdminOrderDetail FromSummary(AdminOrderSummary summary, List<OrderItem> items)
        {
            return new AdminOrderDetail
            {
                Id = summary.Id,
                UserDisplayName = summary.UserDisplayName,
                UserEmail = summary.UserEmail,
                TotalAmount = summary.TotalAmount,
                Status = summary.Status,
                OrderedAt = summary.OrderedAt,
                DeliveryPlace = summary.DeliveryPlace,
                DeliveryAt = summary.DeliveryAt,
                Items = items
            };
        }
    }
}
