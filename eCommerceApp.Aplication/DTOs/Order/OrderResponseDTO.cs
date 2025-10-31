namespace eCommerceApp.Application.DTOs.Order
{
    public class OrderResponseDTO
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; }
        public string ShopName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
