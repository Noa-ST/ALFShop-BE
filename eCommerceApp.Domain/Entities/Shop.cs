using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Shop : AuditableEntity
    {
        [Required]
        public string SellerId { get; set; } = null!; // FK -> User.Id

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
        public string? Logo { get; set; }
        // Dùng float để phù hợp với DTO và việc tính điểm trung bình
        [Range(0.0, 5.0)]
        public float AverageRating { get; set; } = 0.0f;

        // Số lượng đánh giá (cần thiết cho độ tin cậy của Rating)
        public int ReviewCount { get; set; } = 0;

        // Navigation
        [ForeignKey(nameof(SellerId))]
        public User? Seller { get; set; }

        public ICollection<Product>? Products { get; set; }
        public ICollection<ShopCategory>? ShopCategories { get; set; }
        public ICollection<Promotion>? Promotions { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}
