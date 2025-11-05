using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class SellerBalanceRepository : ISellerBalanceRepository
    {
        private readonly AppDbContext _context;

        public SellerBalanceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SellerBalance?> GetByShopIdAsync(Guid shopId)
        {
            return await _context.SellerBalances
                .Include(sb => sb.Shop)
                .Include(sb => sb.Seller)
                .FirstOrDefaultAsync(sb => sb.ShopId == shopId);
        }

        public async Task<SellerBalance?> GetBySellerIdAsync(string sellerId)
        {
            return await _context.SellerBalances
                .Include(sb => sb.Shop)
                .Include(sb => sb.Seller)
                .FirstOrDefaultAsync(sb => sb.SellerId == sellerId);
        }

        public async Task<SellerBalance> GetOrCreateByShopIdAsync(Guid shopId, string sellerId)
        {
            var balance = await GetByShopIdAsync(shopId);
            if (balance != null)
                return balance;

            // Tạo mới nếu chưa có
            balance = new SellerBalance
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                ShopId = shopId,
                AvailableBalance = 0,
                PendingBalance = 0,
                TotalEarned = 0,
                TotalWithdrawn = 0,
                TotalPendingWithdrawal = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await AddAsync(balance);
            return balance;
        }

        public async Task AddAsync(SellerBalance balance)
        {
            await _context.SellerBalances.AddAsync(balance);
            // ✅ Không tự động save - để UnitOfWork quản lý
        }

        public async Task UpdateAsync(SellerBalance balance)
        {
            balance.UpdatedAt = DateTime.UtcNow;
            _context.SellerBalances.Update(balance);
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask;
        }

        public async Task<bool> UpdateBalanceAsync(
            Guid shopId,
            decimal? availableBalanceChange = null,
            decimal? pendingBalanceChange = null,
            decimal? totalEarnedChange = null,
            decimal? totalWithdrawnChange = null,
            decimal? totalPendingWithdrawalChange = null)
        {
            var balance = await _context.SellerBalances
                .FirstOrDefaultAsync(sb => sb.ShopId == shopId);

            if (balance == null)
                return false;

            // Atomic update
            if (availableBalanceChange.HasValue)
                balance.AvailableBalance += availableBalanceChange.Value;

            if (pendingBalanceChange.HasValue)
                balance.PendingBalance += pendingBalanceChange.Value;

            if (totalEarnedChange.HasValue)
                balance.TotalEarned += totalEarnedChange.Value;

            if (totalWithdrawnChange.HasValue)
                balance.TotalWithdrawn += totalWithdrawnChange.Value;

            if (totalPendingWithdrawalChange.HasValue)
                balance.TotalPendingWithdrawal += totalPendingWithdrawalChange.Value;

            balance.UpdatedAt = DateTime.UtcNow;
            
            // ✅ Không tự động save - để UnitOfWork quản lý
            return true;
        }

        public async Task<(IEnumerable<SellerBalance> Balances, int TotalCount)> GetBalancesAsync(
            string? sellerId = null,
            Guid? shopId = null,
            decimal? minAvailableBalance = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.SellerBalances
                .Include(sb => sb.Shop)
                .Include(sb => sb.Seller)
                .AsQueryable();

            if (!string.IsNullOrEmpty(sellerId))
                query = query.Where(sb => sb.SellerId == sellerId);

            if (shopId.HasValue)
                query = query.Where(sb => sb.ShopId == shopId.Value);

            if (minAvailableBalance.HasValue)
                query = query.Where(sb => sb.AvailableBalance >= minAvailableBalance.Value);

            var totalCount = await query.CountAsync();

            var balances = await query
                .OrderByDescending(sb => sb.TotalEarned)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (balances, totalCount);
        }
    }
}

