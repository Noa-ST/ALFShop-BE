using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class CreateProduct : ProductBase
    {
        [Required]
        public Guid ShopId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }
        public List<string>? ImageUrls { get; set; } = new();
    }
}
