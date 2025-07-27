namespace OnigiriShop.Data.Models
{
    public class StatsResult
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int UniqueCustomers { get; set; }
        public string TopProductName { get; set; } = string.Empty;
        public int TopProductQuantity { get; set; }
    }
}
