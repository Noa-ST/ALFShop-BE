namespace eCommerceApp.Aplication.DTOs.Order
{
    public class OrderItemCreateDTO
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

