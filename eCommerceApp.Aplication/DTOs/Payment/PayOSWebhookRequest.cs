namespace eCommerceApp.Aplication.DTOs.Payment
{
    /// <summary>
    /// Webhook request từ PayOS
    /// </summary>
    public class PayOSWebhookRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public PayOSWebhookData? Data { get; set; }
        public string Signature { get; set; } = string.Empty; // Checksum để verify
    }

    public class PayOSWebhookData
    {
        public int OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty; // TransactionId
        public string TransactionDateTime { get; set; } = string.Empty;
        public string Currency { get; set; } = "VND";
        public string PaymentLinkId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // "00" = thành công
        public string Desc { get; set; } = string.Empty;
        public int CounterAccountBankId { get; set; }
        public string CounterAccountBankName { get; set; } = string.Empty;
        public string CounterAccountName { get; set; } = string.Empty;
        public string CounterAccountNumber { get; set; } = string.Empty;
        public string VirtualAccountName { get; set; } = string.Empty;
        public string VirtualAccountNumber { get; set; } = string.Empty;
    }
}

