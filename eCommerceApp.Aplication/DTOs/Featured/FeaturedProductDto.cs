namespace eCommerceApp.Aplication.DTOs.Featured
{
    public class FeaturedProductDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid GlobalCategoryId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public float DiscountPercent { get; set; }
        public int StockQuantity { get; set; }
        public float AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public double RankingScore { get; set; }
        public double? FeaturedWeight { get; set; }
        public bool IsPinned { get; set; }
    }
}