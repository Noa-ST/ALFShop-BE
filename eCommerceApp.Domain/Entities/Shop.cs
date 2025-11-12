using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Shop : AuditableEntity
    {
        [Required]
        public string SellerId { get; set; } = null!;
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Logo { get; set; }
        [Range(0.0, 5.0)]
        public float AverageRating { get; set; } = 0.0f;
        public int ReviewCount { get; set; } = 0;

        // ✅ [BỔ SUNG THÔNG TIN ĐỊA CHỈ SHOP]
        [Required]
        public string Street { get; set; } = null!; // Đường, Phường/Xã
        
        [Required]
        public string City { get; set; } = null!; // Tỉnh/Thành phố (Dùng cho Lọc)
        
        public string? Country { get; set; } = "Việt Nam"; // Mặc định

        // Navigation
        [ForeignKey(nameof(SellerId))]
        public User? Seller { get; set; }

        public ICollection<Product>? Products { get; set; }
        public ICollection<ShopCategory>? ShopCategories { get; set; }
        public ICollection<Promotion>? Promotions { get; set; }
        public ICollection<Message>? Messages { get; set; }

        // Featured fields
        public bool IsPinned { get; set; } = false;
        public double? FeaturedWeight { get; set; }
        public double RankingScore { get; set; } = 0;
        public DateTime? PinnedUntil { get; set; }

        // Ranking metrics
        public float FulfilledRate { get; set; } = 0.0f;
        public float ReturnRate { get; set; } = 0.0f;
        public bool OnlineStatus { get; set; } = false;
        public int AverageResponseTimeSeconds { get; set; } = 0;
    }
}
