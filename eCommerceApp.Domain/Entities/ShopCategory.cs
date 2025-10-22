using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Domain.Entities
{
    public class ShopCategory : AuditableEntity
    {
        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        // --- Liên kết bắt buộc với Shop ---
        [Required]
        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;
        // ----------------------------------

        // --- Cấu trúc Phân cấp (Tùy chọn cho Seller) ---
        public Guid? ParentId { get; set; }
        public ShopCategory? Parent { get; set; }
        public ICollection<ShopCategory>? Children { get; set; }
    }
}
