using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Featured
{
    public class FeaturedPinRequest
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(32)]
        public string Type { get; set; } = null!; // product|shop|category

        public double? Weight { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}