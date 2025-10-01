using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Cart
    {
        [Key]
        public Guid CartId { get; set; }

        public string CustomerId { get; set; } = null!; // FK -> User.Id

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        public ICollection<CartItem>? Items { get; set; }
    }
}
