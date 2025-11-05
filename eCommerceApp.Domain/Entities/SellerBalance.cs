using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerceApp.Domain.Entities.Identity;

namespace eCommerceApp.Domain.Entities
{
    /// <summary>
    /// Số dư và thông tin tài chính của Seller/Shop
    /// </summary>
    public class SellerBalance
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// ID của Seller (User.Id)
        /// </summary>
        [Required]
        public string SellerId { get; set; } = null!;

        /// <summary>
        /// ID của Shop (một Seller có thể có nhiều Shop, mỗi Shop một balance)
        /// </summary>
        [Required]
        public Guid ShopId { get; set; }

        /// <summary>
        /// Số tiền có thể rút ngay (từ các orders đã Delivered và đủ điều kiện)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AvailableBalance { get; set; } = 0;

        /// <summary>
        /// Số tiền chờ giải ngân (từ các orders đã Delivered nhưng chưa đến thời gian hold period)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PendingBalance { get; set; } = 0;

        /// <summary>
        /// Tổng số tiền đã kiếm được (từ tất cả orders đã Delivered)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEarned { get; set; } = 0;

        /// <summary>
        /// Tổng số tiền đã rút (từ tất cả settlements đã Completed)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWithdrawn { get; set; } = 0;

        /// <summary>
        /// Tổng số tiền đang chờ giải ngân (các settlements đã Approved/Processing nhưng chưa Completed)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPendingWithdrawal { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(SellerId))]
        public User? Seller { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }
    }
}

