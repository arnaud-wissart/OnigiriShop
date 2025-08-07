namespace OnigiriShop.Data.Models
{
    public class ProductStat
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ProductStatsResult
    {
        public List<ProductStat> ProductStats { get; set; } = [];
        public string TopProductName { get; set; } = string.Empty;
        public int TopProductQuantity { get; set; }
    }
}
