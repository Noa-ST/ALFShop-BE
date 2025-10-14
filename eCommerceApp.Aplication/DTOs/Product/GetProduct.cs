using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Aplication.DTOs.Product
{
    public class GetProduct : ProductBase
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid CategoryId { get; set; }

        public string? ShopName { get; set; }
        public string? CategoryName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ProductStatus Status { get; set; }
        public List<ProductImageDto> ProductImages { get; set; } = new List<ProductImageDto>();
        public ShopForProductDetail Shop { get; set; } = null!;
    }
}
