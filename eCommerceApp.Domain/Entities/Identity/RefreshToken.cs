using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Domain.Entities.Identity
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        public string UserId { get; set; } = null!; // FK -> User.Id
        public string Token { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
