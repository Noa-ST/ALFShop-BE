using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Domain.Entities
{
    // GlobalCategory: Danh mục chuẩn hóa toàn cầu, quản lý bởi Admin.
    // Dùng cho phân loại sản phẩm chính, tìm kiếm, và mục đích thống kê.
    public class GlobalCategory : AuditableEntity
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        // --- Cấu trúc Phân cấp (Hierarchical) ---
        // Danh mục cha (Nếu là null, đây là danh mục cấp cao nhất)
        public Guid? ParentId { get; set; }
        public GlobalCategory? Parent { get; set; }

        // Danh sách danh mục con
        public ICollection<GlobalCategory>? Children { get; set; }
        // ----------------------------------------

        public ICollection<Product>? Products { get; set; }

        // Featured fields
        public bool IsPinned { get; set; } = false;
        public double? FeaturedWeight { get; set; }
        public double RankingScore { get; set; } = 0;
        public DateTime? PinnedUntil { get; set; }
    }
}