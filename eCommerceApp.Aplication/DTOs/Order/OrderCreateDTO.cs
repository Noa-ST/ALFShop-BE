namespace eCommerceApp.Application.DTOs.Order
{
    public class OrderCreateDTO
    {
        public Guid ShopId { get; set; }
        public List<Guid> CartItemIds { get; set; }
        public string PaymentMethod { get; set; } // COD / Online
    }
}
