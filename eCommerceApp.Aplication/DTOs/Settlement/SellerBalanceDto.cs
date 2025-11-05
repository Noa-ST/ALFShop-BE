namespace eCommerceApp.Aplication.DTOs.Settlement
{
    /// <summary>
    /// DTO cho SellerBalance
    /// </summary>
    public class SellerBalanceDto
    {
        public Guid Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string? SellerName { get; set; }
        public Guid ShopId { get; set; }
        public string? ShopName { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal TotalPendingWithdrawal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

