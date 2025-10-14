using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Promotion : AuditableEntity
    {
        public Guid? ShopId { get; set; } // Áp dụng cho Shop (Nếu ProductId là null)
        public Guid? ProductId { get; set; } // Áp dụng cho Product (Nếu ShopId là null)

        [Required]
        public string Code { get; set; } = null!; // Mã code (e.g., GIAMGIA30)

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } // Giá trị giảm (tiền mặt hoặc %)

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // --- BỔ SUNG TỪ GIAI ĐOẠN 7 ---
        public bool IsActive { get; set; } = true;

        public int MaxUsageCount { get; set; } // Số lần sử dụng tối đa
        public int CurrentUsageCount { get; set; } = 0; // Số lần đã được sử dụng

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0; // Giá trị Order tối thiểu để áp dụng
        // ---------------------------------

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}