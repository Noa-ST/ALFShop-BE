namespace eCommerceApp.Aplication.DTOs.Shop
{
    public class GetShop : ShopBase
    {
        public Guid Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string? SellerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
