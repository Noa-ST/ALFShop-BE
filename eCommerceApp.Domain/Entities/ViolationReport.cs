using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class ViolationReport
    {
        [Key]
        public Guid ReportId { get; set; }

        public Guid? ProductId { get; set; }
        public string? UserId { get; set; } // reporter (nullable)
        public string? AdminId { get; set; } // assigned admin (nullable)

        public string Description { get; set; } = null!;
        public ViolationStatus Status { get; set; } = ViolationStatus.Pending;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? Reporter { get; set; }

        [ForeignKey(nameof(AdminId))]
        public User? Admin { get; set; }
    }
}
