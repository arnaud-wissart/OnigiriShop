using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Data.Models
{
    public class Delivery
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Le lieu est obligatoire.")]
        public string Place { get; set; } = string.Empty;
        public DateTime DeliveryAt { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrenceFrequency? RecurrenceFrequency { get; set; } // enum: Day, Week, Month
        public int? RecurrenceInterval { get; set; } // tous les N jours/semaines/mois
        public string? Comment { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
    }
    public enum RecurrenceFrequency
    {
        Day = 1,
        Week = 2,
        Month = 3
    }
}