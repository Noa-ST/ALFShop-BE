using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Settlement
{
    /// <summary>
    /// DTO để Seller yêu cầu giải ngân
    /// </summary>
    public class CreateSettlementRequest
    {
        [Required(ErrorMessage = "Số tiền không được để trống.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Phương thức giải ngân không được để trống.")]
        public string Method { get; set; } = string.Empty; // BankTransfer, PayOS, Wallet

        /// <summary>
        /// Số tài khoản ngân hàng (nếu Method = BankTransfer)
        /// </summary>
        public string? BankAccount { get; set; }

        /// <summary>
        /// Tên ngân hàng
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// Tên chủ tài khoản
        /// </summary>
        public string? AccountHolderName { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        public string? Notes { get; set; }
    }
}

