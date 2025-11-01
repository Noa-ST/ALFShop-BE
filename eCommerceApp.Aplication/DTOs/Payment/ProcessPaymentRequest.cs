using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Payment
{
    /// <summary>
    /// DTO để process payment
    /// </summary>
    public class ProcessPaymentRequest
    {
        [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
        public string Method { get; set; } = string.Empty; // COD, Cash, Wallet, Bank
    }
}

