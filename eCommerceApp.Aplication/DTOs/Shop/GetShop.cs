namespace eCommerceApp.Aplication.DTOs.Shop
{
    public class GetShop : ShopBase
    {
        public Guid Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string? SellerName { get; set; }
        public float AverageRating { get; set; } // ✅ New: Rating của shop
        public int ReviewCount { get; set; } // ✅ New: Số lượng reviews
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
