namespace eCommerceApp.Aplication.DTOs.Order
{
    public class ShopRevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public RevenueByMethodDto ByMethod { get; set; } = new RevenueByMethodDto();
        public OrdersStatsDto Orders { get; set; } = new OrdersStatsDto();
        public decimal Aov { get; set; }
        public List<RevenueTimeseriesPointDto> Timeseries { get; set; } = new();
    }

    public class RevenueByMethodDto
    {
        public decimal Cash { get; set; }
        public decimal Cod { get; set; }
        public decimal Bank { get; set; }
        public decimal Wallet { get; set; }
    }

    public class OrdersStatsDto
    {
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int RefundedOrders { get; set; } // Hiện chưa dùng (chưa có PaymentStatus.Refunded)
    }

    public class RevenueTimeseriesPointDto
    {
        // Format: yyyy-MM-dd (start of day) hoặc start-of-week (yyyy-MM-dd) nếu groupBy=week
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public int PaidOrders { get; set; }
    }
}