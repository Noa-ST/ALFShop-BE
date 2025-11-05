using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class SettlementRepository : ISettlementRepository
    {
        private readonly AppDbContext _context;

        public SettlementRepository(AppDbContext context)
        {
            _context = context;
        }

        // ========== Settlement Methods ==========

        public async Task<Settlement?> GetByIdAsync(Guid settlementId)
        {
            return await _context.Settlements
                .Include(s => s.Seller)
                .Include(s => s.Shop)
                .Include(s => s.ProcessedByUser)
                .Include(s => s.OrderSettlements)!
                    .ThenInclude(os => os.Order)
                .FirstOrDefaultAsync(s => s.Id == settlementId);
        }

        public async Task<(IEnumerable<Settlement> Settlements, int TotalCount)> GetSettlementsAsync(
            string? sellerId = null,
            Guid? shopId = null,
            SettlementStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Settlements
                .Include(s => s.Shop)
                .Include(s => s.Seller)
                .AsQueryable();

            if (!string.IsNullOrEmpty(sellerId))
                query = query.Where(s => s.SellerId == sellerId);

            if (shopId.HasValue)
                query = query.Where(s => s.ShopId == shopId.Value);

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(s => s.RequestedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.RequestedAt <= endDate.Value);

            var totalCount = await query.CountAsync();

            var settlements = await query
                .OrderByDescending(s => s.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (settlements, totalCount);
        }

        public async Task<IEnumerable<Settlement>> GetPendingSettlementsAsync()
        {
            return await _context.Settlements
                .Include(s => s.Shop)
                .Include(s => s.Seller)
                .Where(s => s.Status == SettlementStatus.Pending)
                .OrderBy(s => s.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Settlement>> GetSettlementsBySellerIdAsync(string sellerId)
        {
            return await _context.Settlements
                .Include(s => s.Shop)
                .Where(s => s.SellerId == sellerId)
                .OrderByDescending(s => s.RequestedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Settlement settlement)
        {
            await _context.Settlements.AddAsync(settlement);
            // ✅ Không tự động save - để UnitOfWork quản lý
        }

        public async Task UpdateAsync(Settlement settlement)
        {
            settlement.UpdatedAt = DateTime.UtcNow;
            _context.Settlements.Update(settlement);
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask;
        }

        // ========== OrderSettlement Methods ==========

        public async Task<OrderSettlement?> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.OrderSettlements
                .Include(os => os.Order)
                .Include(os => os.Settlement)
                .FirstOrDefaultAsync(os => os.OrderId == orderId);
        }

        public async Task<IEnumerable<OrderSettlement>> GetOrderSettlementsBySettlementIdAsync(Guid settlementId)
        {
            return await _context.OrderSettlements
                .Include(os => os.Order)
                    .ThenInclude(o => o!.Items)
                .Where(os => os.SettlementId == settlementId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetEligibleOrdersForSettlementAsync(
            Guid shopId,
            int holdPeriodDays = 3)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-holdPeriodDays);

            // Lấy các orders:
            // - Thuộc shop này
            // - Đã Delivered
            // - Đã Paid
            // - Delivered cách đây >= holdPeriodDays ngày
            // - Chưa được settle
            var settledOrderIds = await _context.OrderSettlements
                .Select(os => os.OrderId)
                .ToListAsync();

            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.ShopId == shopId
                    && o.Status == OrderStatus.Delivered
                    && o.PaymentStatus == PaymentStatus.Paid
                    && (o.UpdatedAt ?? o.CreatedAt) <= cutoffDate // Use UpdatedAt hoặc CreatedAt nếu UpdatedAt null
                    && !settledOrderIds.Contains(o.Id))
                .ToListAsync();
        }

        public async Task AddOrderSettlementAsync(OrderSettlement orderSettlement)
        {
            await _context.OrderSettlements.AddAsync(orderSettlement);
            // ✅ Không tự động save - để UnitOfWork quản lý
        }

        public async Task AddOrderSettlementsAsync(IEnumerable<OrderSettlement> orderSettlements)
        {
            await _context.OrderSettlements.AddRangeAsync(orderSettlements);
            // ✅ Không tự động save - để UnitOfWork quản lý
        }

        public async Task<Dictionary<SettlementStatus, int>> GetSettlementsByStatusAsync()
        {
            return await _context.Settlements
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<decimal> GetTotalSettledAmountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Settlements
                .Where(s => s.Status == SettlementStatus.Completed);

            if (startDate.HasValue)
                query = query.Where(s => s.CompletedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.CompletedAt <= endDate.Value);

            return await query.SumAsync(s => s.NetAmount);
        }
    }
}

