namespace eCommerceApp.Aplication.DTOs.Settlement
{
    /// <summary>
    /// DTO cho Settlement response
    /// </summary>
    public class SettlementDto
    {
        public Guid Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string? SellerName { get; set; }
        public Guid ShopId { get; set; }
        public string? ShopName { get; set; }
        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
        public string? ProcessedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? FailureReason { get; set; }
        public List<OrderSettlementDto>? OrderSettlements { get; set; }
    }
}

