using eCommerceApp.Domain.Enums; // <-- THÊM DÒNG NÀY (ĐÂY LÀ LỖI CỦA BẠN)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Promotion : AuditableEntity
    {
        public Guid? ShopId { get; set; }
        public Guid? ProductId { get; set; }

        [Required]
        public string Code { get; set; } = null!;

        [Required]
        public PromotionType PromotionType { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public int MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}