using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Repositories
{
    /// <summary>
    /// Repository interface cho Settlement và OrderSettlement
    /// </summary>
    public interface ISettlementRepository
    {
        // ========== Settlement Methods ==========
        
        /// <summary>
        /// Lấy Settlement theo ID
        /// </summary>
        Task<Settlement?> GetByIdAsync(Guid settlementId);
        
        /// <summary>
        /// Lấy danh sách Settlements với filter
        /// </summary>
        Task<(IEnumerable<Settlement> Settlements, int TotalCount)> GetSettlementsAsync(
            string? sellerId = null,
            Guid? shopId = null,
            SettlementStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20);
        
        /// <summary>
        /// Lấy danh sách pending settlements (cho Admin)
        /// </summary>
        Task<IEnumerable<Settlement>> GetPendingSettlementsAsync();
        
        /// <summary>
        /// Lấy settlements của một seller
        /// </summary>
        Task<IEnumerable<Settlement>> GetSettlementsBySellerIdAsync(string sellerId);
        
        /// <summary>
        /// Thêm Settlement mới
        /// </summary>
        Task AddAsync(Settlement settlement);
        
        /// <summary>
        /// Cập nhật Settlement
        /// </summary>
        Task UpdateAsync(Settlement settlement);
        
        // ========== OrderSettlement Methods ==========
        
        /// <summary>
        /// Lấy OrderSettlement theo OrderId
        /// </summary>
        Task<OrderSettlement?> GetByOrderIdAsync(Guid orderId);
        
        /// <summary>
        /// Lấy danh sách OrderSettlements theo SettlementId
        /// </summary>
        Task<IEnumerable<OrderSettlement>> GetOrderSettlementsBySettlementIdAsync(Guid settlementId);
        
        /// <summary>
        /// Lấy danh sách orders chưa được settle (eligible for settlement)
        /// </summary>
        Task<IEnumerable<Order>> GetEligibleOrdersForSettlementAsync(
            Guid shopId,
            int holdPeriodDays = 3);
        
        /// <summary>
        /// Thêm OrderSettlement
        /// </summary>
        Task AddOrderSettlementAsync(OrderSettlement orderSettlement);
        
        /// <summary>
        /// Thêm nhiều OrderSettlements cùng lúc
        /// </summary>
        Task AddOrderSettlementsAsync(IEnumerable<OrderSettlement> orderSettlements);
        
        /// <summary>
        /// Lấy thống kê settlement
        /// </summary>
        Task<Dictionary<SettlementStatus, int>> GetSettlementsByStatusAsync();
        
        /// <summary>
        /// Lấy tổng số tiền đã giải ngân
        /// </summary>
        Task<decimal> GetTotalSettledAmountAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}

