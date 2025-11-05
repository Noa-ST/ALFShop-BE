using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Entities
{
    /// <summary>
    /// Liên kết giữa Order và Settlement
    /// Một Settlement có thể chứa nhiều Orders
    /// </summary>
    public class OrderSettlement
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// ID của Order
        /// </summary>
        [Required]
        public Guid OrderId { get; set; }

        /// <summary>
        /// ID của Settlement
        /// </summary>
        [Required]
        public Guid SettlementId { get; set; }

        /// <summary>
        /// Tổng tiền của order (Order.TotalAmount tại thời điểm settlement)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderAmount { get; set; }

        /// <summary>
        /// Phí platform tính trên order này (ví dụ: 5% của OrderAmount)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Commission { get; set; } = 0;

        /// <summary>
        /// Số tiền seller nhận từ order này = OrderAmount - Commission
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SettlementAmount { get; set; }

        /// <summary>
        /// Thời gian order được delivered (để tính hold period)
        /// </summary>
        public DateTime? OrderDeliveredAt { get; set; }

        /// <summary>
        /// Thời gian order đủ điều kiện để giải ngân (sau hold period)
        /// </summary>
        public DateTime? EligibleAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        [ForeignKey(nameof(SettlementId))]
        public Settlement? Settlement { get; set; }
    }
}

