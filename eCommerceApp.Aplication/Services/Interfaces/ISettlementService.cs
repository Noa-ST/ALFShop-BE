using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Settlement;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Settlement/Payout system
    /// </summary>
    public interface ISettlementService
    {
        // ========== Seller Methods ==========
        
        /// <summary>
        /// Lấy số dư của Seller
        /// </summary>
        Task<ServiceResponse<SellerBalanceDto>> GetSellerBalanceAsync(string sellerId);
        
        /// <summary>
        /// Tạo yêu cầu giải ngân
        /// </summary>
        Task<ServiceResponse<SettlementDto>> CreateSettlementRequestAsync(string sellerId, CreateSettlementRequest request);
        
        /// <summary>
        /// Lấy danh sách settlements của Seller
        /// </summary>
        Task<ServiceResponse<PagedResult<SettlementDto>>> GetMySettlementsAsync(string sellerId, SettlementFilterDto? filter = null);
        
        // ========== Admin Methods ==========
        
        /// <summary>
        /// Lấy danh sách pending settlements (cho Admin)
        /// </summary>
        Task<ServiceResponse<List<SettlementDto>>> GetPendingSettlementsAsync();
        
        /// <summary>
        /// Approve settlement
        /// </summary>
        Task<ServiceResponse<bool>> ApproveSettlementAsync(Guid settlementId, string adminId);
        
        /// <summary>
        /// Process payout (thực hiện giải ngân qua PayOS/Bank)
        /// </summary>
        Task<ServiceResponse<bool>> ProcessPayoutAsync(Guid settlementId, string adminId, ProcessSettlementRequest request);
        
        /// <summary>
        /// Reject/Cancel settlement
        /// </summary>
        Task<ServiceResponse<bool>> RejectSettlementAsync(Guid settlementId, string adminId, string reason);
        
        /// <summary>
        /// Lấy tất cả settlements với filter (cho Admin)
        /// </summary>
        Task<ServiceResponse<PagedResult<SettlementDto>>> GetAllSettlementsAsync(SettlementFilterDto? filter = null);
        
        /// <summary>
        /// Tính toán và tạo settlement cho order khi Delivered (auto-settlement)
        /// </summary>
        Task<ServiceResponse> CalculateSettlementForOrderAsync(Guid orderId);
        
        /// <summary>
        /// Lấy thống kê settlement
        /// </summary>
        Task<ServiceResponse<object>> GetSettlementStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Complete settlement sau khi PayOS confirm transfer thành công (hoặc manual confirm)
        /// </summary>
        Task<ServiceResponse<bool>> CompleteSettlementAsync(Guid settlementId, string adminId);
    }
}

