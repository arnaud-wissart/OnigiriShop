namespace OnigiriShop.Data.Models
{
    public class AdminOrderSummary
    {
        public int Id { get; set; }
        public string UserDisplayName { get; set; }
        public string UserEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime OrderedAt { get; set; }
        public string DeliveryPlace { get; set; }
        public DateTime DeliveryAt { get; set; }
    }
}