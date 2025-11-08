using System.Collections.Generic;
using System.Linq;

namespace eCommerceApp.Aplication.DTOs.Order
{
    public class OrderResponseDTO
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? PromotionCodeUsed { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IEnumerable<OrderItemResponseDTO> Items { get; set; } = Enumerable.Empty<OrderItemResponseDTO>();
    }
}
