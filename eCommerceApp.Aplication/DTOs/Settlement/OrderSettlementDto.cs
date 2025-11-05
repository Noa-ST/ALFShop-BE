namespace eCommerceApp.Aplication.DTOs.Settlement
{
    /// <summary>
    /// DTO cho OrderSettlement
    /// </summary>
    public class OrderSettlementDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid SettlementId { get; set; }
        public decimal OrderAmount { get; set; }
        public decimal Commission { get; set; }
        public decimal SettlementAmount { get; set; }
        public DateTime? OrderDeliveredAt { get; set; }
        public DateTime? EligibleAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

