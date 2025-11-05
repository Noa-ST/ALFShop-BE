using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Interfaces
{
    /// <summary>
    /// Repository interface cho SellerBalance
    /// </summary>
    public interface ISellerBalanceRepository
    {
        /// <summary>
        /// Lấy SellerBalance theo ShopId
        /// </summary>
        Task<SellerBalance?> GetByShopIdAsync(Guid shopId);
        
        /// <summary>
        /// Lấy SellerBalance theo SellerId
        /// </summary>
        Task<SellerBalance?> GetBySellerIdAsync(string sellerId);
        
        /// <summary>
        /// Tạo hoặc lấy SellerBalance (nếu chưa có thì tạo mới)
        /// </summary>
        Task<SellerBalance> GetOrCreateByShopIdAsync(Guid shopId, string sellerId);
        
        /// <summary>
        /// Thêm SellerBalance mới
        /// </summary>
        Task AddAsync(SellerBalance balance);
        
        /// <summary>
        /// Cập nhật SellerBalance
        /// </summary>
        Task UpdateAsync(SellerBalance balance);
        
        /// <summary>
        /// Cập nhật số dư (atomic operation)
        /// </summary>
        Task<bool> UpdateBalanceAsync(
            Guid shopId,
            decimal? availableBalanceChange = null,
            decimal? pendingBalanceChange = null,
            decimal? totalEarnedChange = null,
            decimal? totalWithdrawnChange = null,
            decimal? totalPendingWithdrawalChange = null);
        
        /// <summary>
        /// Lấy danh sách SellerBalance với filter
        /// </summary>
        Task<(IEnumerable<SellerBalance> Balances, int TotalCount)> GetBalancesAsync(
            string? sellerId = null,
            Guid? shopId = null,
            decimal? minAvailableBalance = null,
            int page = 1,
            int pageSize = 20);
    }
}

