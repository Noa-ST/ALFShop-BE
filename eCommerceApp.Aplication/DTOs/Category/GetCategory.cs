using eCommerceApp.Aplication.DTOs.Product;

namespace eCommerceApp.Aplication.DTOs.Category
{
    public class GetCategory : CategoryBase
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string? ShopName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<GetProduct>? Products { get; set; }
    }
}
