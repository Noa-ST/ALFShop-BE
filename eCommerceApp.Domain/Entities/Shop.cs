using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Shop : AuditableEntity
    {
        [Required]
        public string SellerId { get; set; } = null!; // FK -> User.Id

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
        public string? Logo { get; set; }

        // Navigation
        [ForeignKey(nameof(SellerId))]
        public User? Seller { get; set; }

        public ICollection<Product>? Products { get; set; }
        public ICollection<Category>? Categories { get; set; }
        public ICollection<Promotion>? Promotions { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}
