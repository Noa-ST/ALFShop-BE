using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Entities
{
    /// <summary>
    /// Lưu lịch sử thay đổi trạng thái thanh toán
    /// </summary>
    public class PaymentHistory
    {
        [Key]
        public Guid HistoryId { get; set; }

        public Guid PaymentId { get; set; }

        public PaymentStatus OldStatus { get; set; }
        public PaymentStatus NewStatus { get; set; }

        public string? Reason { get; set; } // Lý do thay đổi (Refund, Webhook, Manual, etc.)

        public string? ChangedBy { get; set; } // UserId hoặc "System" hoặc "PayOS"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(PaymentId))]
        public Payment? Payment { get; set; }
    }
}

