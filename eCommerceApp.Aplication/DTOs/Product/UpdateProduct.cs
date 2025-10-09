using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class UpdateProduct : ProductBase
    {
        [Required]
        public Guid CategoryId { get; set; }

        public ProductStatus? Status { get; set; }

        public List<string>? ImageUrls { get; set; } = new();
    }
}
