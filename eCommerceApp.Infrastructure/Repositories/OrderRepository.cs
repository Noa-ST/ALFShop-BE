using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            // ✅ Không tự động save - để UnitOfWork quản lý
            return order;
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId)
            => await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)!
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetOrdersByShopIdAsync(Guid shopId)
            => await _context.Orders
                .Where(o => o.ShopId == shopId)
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)!
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
            => await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)! // ✅ Fix: Include Items
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

        public async Task<Order?> GetByIdAsync(Guid id) // ✅ Fix: Return nullable
            => await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)!
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task UpdateStatusAsync(Guid orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new Exception("Order not found");

            if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
                throw new Exception($"Invalid order status: {status}");

            order.Status = parsedStatus;
            order.UpdatedAt = DateTime.UtcNow; // ✅ Fix: Update UpdatedAt
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask;
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask;
        }

        // ✅ New: Pagination & Filtering
        public async Task<(IEnumerable<Order> Orders, int TotalCount)> SearchAndFilterAsync(
            string? keyword,
            OrderStatus? status,
            Guid? shopId,
            string? customerId,
            DateTime? startDate,
            DateTime? endDate,
            decimal? minAmount,
            decimal? maxAmount,
            string sortBy,
            string sortOrder,
            int page,
            int pageSize)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)!
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(o =>
                    o.Id.ToString().Contains(keyword) ||
                    (o.Customer != null && o.Customer.UserName != null && o.Customer.UserName.ToLower().Contains(keyword)) ||
                    (o.Shop != null && o.Shop.Name != null && o.Shop.Name.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(o.TrackingNumber) && o.TrackingNumber.ToLower().Contains(keyword)));
            }

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (shopId.HasValue)
                query = query.Where(o => o.ShopId == shopId.Value);

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(o => o.CustomerId == customerId);

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1)); // Include full day

            if (minAmount.HasValue)
                query = query.Where(o => o.TotalAmount >= minAmount.Value);

            if (maxAmount.HasValue)
                query = query.Where(o => o.TotalAmount <= maxAmount.Value);

            // Get total count before pagination
            int totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "totalamount" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(o => o.TotalAmount)
                    : query.OrderByDescending(o => o.TotalAmount),
                "status" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(o => o.Status)
                    : query.OrderByDescending(o => o.Status),
                "updatedat" => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(o => o.UpdatedAt)
                    : query.OrderByDescending(o => o.UpdatedAt),
                _ => sortOrder.ToLower() == "asc"
                    ? query.OrderBy(o => o.CreatedAt)
                    : query.OrderByDescending(o => o.CreatedAt)
            };

            // Apply pagination
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        // ✅ New: Statistics
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(OrderStatus status)
        {
            return await _context.Orders.CountAsync(o => o.Status == status);
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));

            return await query.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        }

        public async Task<decimal> GetAverageOrderValueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));

            var count = await query.CountAsync();
            if (count == 0) return 0;

            var total = await query.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            return total / count;
        }

        public async Task<Dictionary<OrderStatus, int>> GetOrdersByStatusAsync()
        {
            return await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<List<(Guid ShopId, string ShopName, int OrderCount, decimal Revenue)>> GetTopShopsByOrdersAsync(int top = 10)
        {
            return await _context.Orders
                .Include(o => o.Shop)
                .Where(o => o.Status == OrderStatus.Delivered)
                .GroupBy(o => o.ShopId)
                .Select(g => new
                {
                    ShopId = g.Key,
                    ShopName = g.First().Shop?.Name ?? "Unknown",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(top)
                .Select(x => new ValueTuple<Guid, string, int, decimal>(
                    x.ShopId,
                    x.ShopName ?? "Unknown",
                    x.OrderCount,
                    x.Revenue))
                .ToListAsync();
        }

        public async Task<List<(string CustomerId, string CustomerName, int OrderCount, decimal TotalSpent)>> GetTopCustomersAsync(int top = 10)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == OrderStatus.Delivered)
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    CustomerName = g.First().Customer?.UserName ?? "Unknown",
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(top)
                .Select(x => new ValueTuple<string, string, int, decimal>(
                    x.CustomerId,
                    x.CustomerName ?? "Unknown",
                    x.OrderCount,
                    x.TotalSpent))
                .ToListAsync();
        }

        // ✅ New: Update Tracking Number
        public async Task UpdateTrackingNumberAsync(Guid orderId, string trackingNumber)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new Exception("Order not found");

            order.TrackingNumber = trackingNumber;
            order.UpdatedAt = DateTime.UtcNow;
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask;
        }
        
        // ✅ New: Create order with transaction support
        public async Task<Order> CreateOrderWithTransactionAsync(Order order, Func<Task>? beforeSave = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Orders.AddAsync(order);
                
                // Execute beforeSave callback if provided (e.g., reduce stock)
                if (beforeSave != null)
                {
                    await beforeSave();
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        
        // ✅ New: Get unpaid orders older than specified time (for auto-cancellation)
        public async Task<IEnumerable<Order>> GetUnpaidOrdersOlderThanAsync(TimeSpan olderThan, PaymentMethod? excludePaymentMethod = null)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(olderThan);
            
            var query = _context.Orders
                .Where(o => o.Status == OrderStatus.Pending
                    && o.PaymentStatus == PaymentStatus.Pending
                    && o.CreatedAt < cutoffTime);
            
            if (excludePaymentMethod.HasValue)
            {
                query = query.Where(o => o.PaymentMethod != excludePaymentMethod.Value);
            }
            
            return await query
                .Include(o => o.Items)! // ✅ thêm null-forgiving cho collection
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();
        }
    }
}
