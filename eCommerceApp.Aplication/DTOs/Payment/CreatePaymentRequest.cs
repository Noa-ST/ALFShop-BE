namespace eCommerceApp.Aplication.DTOs.Payment
{
    public class CreatePaymentRequest
    {
        public Guid OrderId { get; set; }
        public string Method { get; set; } = string.Empty;
    }
}
