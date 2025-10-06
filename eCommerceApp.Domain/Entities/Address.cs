using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Address : AuditableEntity
    {
        public string UserId { get; set; } = null!; // FK -> User.Id

        [Required]
        public string Street { get; set; } = null!;

        [Required]
        public string City { get; set; } = null!;

        [Required]
        public string Country { get; set; } = null!;

        public bool IsDefault { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
