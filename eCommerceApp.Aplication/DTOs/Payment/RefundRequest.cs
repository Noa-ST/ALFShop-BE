namespace eCommerceApp.Aplication.DTOs.Payment
{
    /// <summary>
    /// Request để hoàn tiền
    /// </summary>
    public class RefundRequest
    {
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; } // Số tiền hoàn (có thể hoàn một phần)
        public string Reason { get; set; } = string.Empty; // Lý do hoàn tiền
    }
}

