using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Shop
    {
        [Key]
        public Guid ShopId { get; set; }

        [Required]
        public string SellerId { get; set; } = null!; // FK -> User.Id

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
        public string? Logo { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(SellerId))]
        public User? Seller { get; set; }

        public ICollection<Product>? Products { get; set; }
        public ICollection<Promotion>? Promotions { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}
