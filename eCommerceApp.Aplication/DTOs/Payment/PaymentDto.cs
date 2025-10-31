using System;

namespace eCommerceApp.Aplication.DTOs.Payment
{
    public class PaymentDto
    {
        public Guid PaymentId { get; set; }
        public Guid OrderId { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
