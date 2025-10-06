using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Cart : AuditableEntity
    {
        public string CustomerId { get; set; } = null!; // FK -> User.Id

        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        public ICollection<CartItem>? Items { get; set; }
    }
}
