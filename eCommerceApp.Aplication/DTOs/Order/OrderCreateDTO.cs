namespace eCommerceApp.Application.DTOs.Order
{
    public class OrderCreateDTO
    {
        public string CustomerId { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public Guid AddressId { get; set; }
        public List<OrderItemCreateDTO> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty; // COD / Wallet / Bank / Cash
        public decimal ShippingFee { get; set; }
        public string? PromotionCode { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}
