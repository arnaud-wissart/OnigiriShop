using System.ComponentModel.DataAnnotations;

namespace OnigiriShop.Data.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis.")]
        [StringLength(100, ErrorMessage = "Nom trop long.")]
        public string Name { get; set; }

        [StringLength(512, ErrorMessage = "Description trop longue.")]
        public string Description { get; set; }

        [Range(0, 1000, ErrorMessage = "Prix incorrect.")]
        public decimal Price { get; set; }

        public bool IsOnMenu { get; set; }

        [StringLength(256)]
        public string ImagePath { get; set; }
        public bool IsDeleted { get; set; }
    }
}