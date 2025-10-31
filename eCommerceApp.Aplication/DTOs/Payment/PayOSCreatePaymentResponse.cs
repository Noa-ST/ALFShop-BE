namespace eCommerceApp.Aplication.DTOs.Payment
{
    /// <summary>
    /// Response từ PayOS khi tạo payment link
    /// </summary>
    public class PayOSCreatePaymentResponse
    {
        public int Code { get; set; }
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
        public long? CheckoutUrl { get; set; }
        public string QrCode { get; set; } = string.Empty;
    }
}

