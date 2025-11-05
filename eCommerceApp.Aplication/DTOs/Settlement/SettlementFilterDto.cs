namespace eCommerceApp.Aplication.DTOs.Settlement
{
    /// <summary>
    /// Filter DTO cho Settlement queries
    /// </summary>
    public class SettlementFilterDto
    {
        public string? SellerId { get; set; }
        public Guid? ShopId { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public void Validate()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > 100) PageSize = 100;
        }
    }
}

