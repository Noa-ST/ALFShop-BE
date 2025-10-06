using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Review : AuditableEntity
    {
        public Guid ProductId { get; set; }
        public string UserId { get; set; } = null!; // FK -> User.Id

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
