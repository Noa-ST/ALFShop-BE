using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Settlement
{
    /// <summary>
    /// DTO để Admin xử lý giải ngân (thực hiện payout)
    /// </summary>
    public class ProcessSettlementRequest
    {
        [Required(ErrorMessage = "Mã tham chiếu giao dịch không được để trống.")]
        [MaxLength(100, ErrorMessage = "Mã tham chiếu không được vượt quá 100 ký tự.")]
        public string TransactionReference { get; set; } = string.Empty;

        /// <summary>
        /// Ghi chú (optional)
        /// </summary>
        public string? Notes { get; set; }
    }
}

