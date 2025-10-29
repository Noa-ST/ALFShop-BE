namespace eCommerceApp.Aplication.DTOs.Cart
{
    public class GetCartDto
    {
        public Guid CartId { get; set; }
        public string CustomerId { get; set; } = null!;
        public decimal SubTotal { get; set; } // Tổng tiền trước thuế/ship/khuyến mãi
        public List<GetCartItemDto> Items { get; set; } = new List<GetCartItemDto>();
    }
}