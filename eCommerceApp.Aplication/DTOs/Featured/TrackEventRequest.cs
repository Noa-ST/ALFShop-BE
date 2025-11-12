using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Featured
{
    public class TrackEventRequest
    {
        [Required]
        [MaxLength(32)]
        public string EntityType { get; set; } = null!; // product|shop|category

        [Required]
        public Guid EntityId { get; set; }

        // Optional tracking fields
        [MaxLength(128)]
        public string? SessionId { get; set; }

        [MaxLength(32)]
        public string? Device { get; set; }

        [MaxLength(64)]
        public string? Region { get; set; }

        [MaxLength(64)]
        public string? City { get; set; }

        public string? MetadataJson { get; set; }
    }
}