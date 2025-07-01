namespace OnigiriShop.Data
{
    public class Order
    {
        public string OrderId { get; set; }
        public string UserId { get; set; }               // id OAuth
        public string UserDisplayName { get; set; }      // username 
        public string UserEmail { get; set; }            // email (si dispo)
        public DateTime OrderDate { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
