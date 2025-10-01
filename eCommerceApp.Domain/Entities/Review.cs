using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Review
    {
        [Key]
        public Guid ReviewId { get; set; }

        public Guid ProductId { get; set; }
        public string UserId { get; set; } = null!; // FK -> User.Id

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
