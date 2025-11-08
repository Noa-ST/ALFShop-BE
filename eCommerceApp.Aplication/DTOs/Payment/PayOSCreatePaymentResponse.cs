namespace eCommerceApp.Aplication.DTOs.Payment
{
    /// <summary>
    /// Response từ PayOS khi tạo payment link
    /// </summary>
    public class PayOSCreatePaymentResponse
    {
        public string Code { get; set; } = string.Empty; // ✅ PayOS trả về code dạng string ("00", "01", etc.)
        public string Desc { get; set; } = string.Empty;
        public PayOSData? Data { get; set; }
    }

    public class PayOSData
    {
        public string Bin { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int OrderCode { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentLinkId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long? ExpiredAt { get; set; }
        public string? CheckoutUrl { get; set; } // ✅ URL để redirect user đến trang thanh toán PayOS
        public string QrCode { get; set; } = string.Empty;
    }
}

