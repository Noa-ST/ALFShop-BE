using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Domain.Entities
{
    public class Category : AuditableEntity
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<Product>? Products { get; set; }
        // Quan hệ với Shop
        public Guid ShopId { get; set; }
        public Shop? Shop { get; set; }
    }
}
