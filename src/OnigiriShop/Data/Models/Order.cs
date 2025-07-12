namespace OnigiriShop.Data.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DeliveryId { get; set; }
        public DateTime OrderedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string Comment { get; set; }
        public string UserDisplayName { get; set; }
        public string UserEmail { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
