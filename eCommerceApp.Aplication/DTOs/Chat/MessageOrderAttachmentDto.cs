using System;

namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class MessageOrderAttachmentDto
    {
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

