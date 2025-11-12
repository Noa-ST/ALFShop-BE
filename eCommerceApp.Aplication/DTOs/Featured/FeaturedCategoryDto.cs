namespace eCommerceApp.Aplication.DTOs.Featured
{
    public class FeaturedCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public double RankingScore { get; set; }
        public double? FeaturedWeight { get; set; }
        public bool IsPinned { get; set; }
    }
}