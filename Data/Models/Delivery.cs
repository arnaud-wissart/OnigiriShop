namespace OnigiriShop.Data.Models
{
    public class Delivery
    {
        public int Id { get; set; }
        public string Place { get; set; }
        public DateTime DeliveryAt { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrenceRule { get; set; }
        public string Comment { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}