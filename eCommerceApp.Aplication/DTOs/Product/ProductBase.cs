using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class ProductBase
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
    }
}
