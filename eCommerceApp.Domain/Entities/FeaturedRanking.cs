using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Domain.Entities
{
    public class FeaturedRanking : AuditableEntity
    {
        // product|shop|category
        [Required]
        [MaxLength(32)]
        public string EntityType { get; set; } = null!;

        [Required]
        public Guid EntityId { get; set; }

        public double Score { get; set; }

        // components for debug/explainability
        public double PinBoost { get; set; }
        public double Metric1 { get; set; }
        public double Metric2 { get; set; }
        public double Metric3 { get; set; }
        public double Penalty { get; set; }

        public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    }
}