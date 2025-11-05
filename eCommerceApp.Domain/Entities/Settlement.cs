using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Entities
{
    /// <summary>
    /// Bản ghi giải ngân cho Seller
    /// </summary>
    public class Settlement
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// ID của Seller
        /// </summary>
        [Required]
        public string SellerId { get; set; } = null!;

        /// <summary>
        /// ID của Shop
        /// </summary>
        [Required]
        public Guid ShopId { get; set; }

        /// <summary>
        /// Tổng số tiền giải ngân (trước khi trừ phí)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Phí platform (nếu có)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformFee { get; set; } = 0;

        /// <summary>
        /// Số tiền thực nhận = Amount - PlatformFee
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        /// <summary>
        /// Trạng thái giải ngân
        /// </summary>
        public SettlementStatus Status { get; set; } = SettlementStatus.Pending;

        /// <summary>
        /// Phương thức giải ngân
        /// </summary>
        public SettlementMethod Method { get; set; } = SettlementMethod.BankTransfer;

        /// <summary>
        /// Số tài khoản ngân hàng (nếu Method = BankTransfer)
        /// </summary>
        [MaxLength(50)]
        public string? BankAccount { get; set; }

        /// <summary>
        /// Tên ngân hàng
        /// </summary>
        [MaxLength(100)]
        public string? BankName { get; set; }

        /// <summary>
        /// Tên chủ tài khoản
        /// </summary>
        [MaxLength(100)]
        public string? AccountHolderName { get; set; }

        /// <summary>
        /// Mã tham chiếu giao dịch từ PayOS hoặc ngân hàng
        /// </summary>
        [MaxLength(100)]
        public string? TransactionReference { get; set; }

        /// <summary>
        /// Ghi chú/Note cho settlement này
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// ID của Admin xử lý settlement này
        /// </summary>
        [MaxLength(450)]
        public string? ProcessedBy { get; set; }

        /// <summary>
        /// Thời gian seller yêu cầu giải ngân
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian Admin approve
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Thời gian bắt đầu xử lý giải ngân
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Thời gian hoàn tất giải ngân
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Lý do nếu settlement bị Failed hoặc Cancelled
        /// </summary>
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(SellerId))]
        public User? Seller { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        [ForeignKey(nameof(ProcessedBy))]
        public User? ProcessedByUser { get; set; }

        /// <summary>
        /// Danh sách các orders được giải ngân trong settlement này
        /// </summary>
        public ICollection<OrderSettlement>? OrderSettlements { get; set; }
    }
}

