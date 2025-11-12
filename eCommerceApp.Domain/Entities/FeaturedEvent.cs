using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class FeaturedEvent : AuditableEntity
    {
        // product|shop|category
        [Required]
        [MaxLength(32)]
        public string EntityType { get; set; } = null!;

        [Required]
        public Guid EntityId { get; set; }

        // click|impression|add_to_cart
        [Required]
        [MaxLength(32)]
        public string EventType { get; set; } = null!;

        // optional user tracking
        [MaxLength(450)]
        public string? UserId { get; set; }

        // optional session/device/region for analysis
        [MaxLength(128)]
        public string? SessionId { get; set; }

        [MaxLength(32)]
        public string? Device { get; set; } // mobile|desktop

        [MaxLength(64)]
        public string? Region { get; set; }

        [MaxLength(64)]
        public string? City { get; set; }

        // payload for future extension (JSON)
        public string? MetadataJson { get; set; }
    }
}