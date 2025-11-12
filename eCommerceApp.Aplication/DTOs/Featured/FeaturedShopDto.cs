namespace eCommerceApp.Aplication.DTOs.Featured
{
    public class FeaturedShopDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public float AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool OnlineStatus { get; set; }
        public float FulfilledRate { get; set; }
        public float ReturnRate { get; set; }
        public bool IsPinned { get; set; }
        public double? FeaturedWeight { get; set; }
        public double RankingScore { get; set; }
    }
}