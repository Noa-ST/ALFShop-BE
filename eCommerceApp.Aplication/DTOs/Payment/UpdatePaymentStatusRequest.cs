namespace eCommerceApp.Aplication.DTOs.Payment
{
    public class UpdatePaymentStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
