namespace eCommerceApp.Aplication.DTOs.Cart
{
    public class GetCartItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ShopName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } 
        public decimal ItemTotal { get; set; }
        public string? ImageUrl { get; set; }
    }
}