using eCommerceApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Product : AuditableEntity
    {
        public Guid ShopId { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Pending;
        public string? Reason { get; set; }
        [Column(TypeName = "float")]
        public float AverageRating { get; set; } = 0.0f;
        public int ReviewCount { get; set; } = 0;
        // Navigation
        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }
        public Guid GlobalCategoryId { get; set; }
        public GlobalCategory GlobalCategory { get; set; } = null!;
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<OrderItem>? OrderItems { get; set; }
        public ICollection<CartItem>? CartItems { get; set; }
        public ICollection<Promotion>? Promotions { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<ViolationReport>? ViolationReports { get; set; }

        // Featured fields
        public bool IsPinned { get; set; } = false;
        public double? FeaturedWeight { get; set; }
        public double RankingScore { get; set; } = 0;
        public DateTime? PinnedUntil { get; set; }

        // Ranking metrics
        [Column(TypeName = "float")]
        public float DiscountPercent { get; set; } = 0.0f;
        public int Views7d { get; set; } = 0;
        public int Views30d { get; set; } = 0;
        public int AddsToCart7d { get; set; } = 0;
        public int AddsToCart30d { get; set; } = 0;
        public int Sold7d { get; set; } = 0;
        public int Sold30d { get; set; } = 0;
    }
}

