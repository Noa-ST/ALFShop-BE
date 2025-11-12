namespace eCommerceApp.Aplication.DTOs.Featured
{
    public class FeaturedScoreDebugDto
    {
        public string Type { get; set; } = null!;
        public Guid Id { get; set; }
        public double Score { get; set; }

        // Mô tả thành phần
        public double PinBoost { get; set; }
        public double MetricComponent1 { get; set; }
        public double MetricComponent2 { get; set; }
        public double MetricComponent3 { get; set; }
        public double RatingComponent { get; set; }
        public double PenaltyComponent { get; set; }
    }
}