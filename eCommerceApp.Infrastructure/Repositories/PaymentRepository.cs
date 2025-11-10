using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            // ✅ Không tự động save - để UnitOfWork quản lý
        }

        public async Task<bool> UpdateStatusAsync(Guid paymentId, string newStatus)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null)
                return false;

            // Chuyển chuỗi sang Enum PaymentStatus
            if (Enum.TryParse<PaymentStatus>(newStatus, true, out var parsedStatus))
            {
                payment.Status = parsedStatus;
            }
            else
            {
                payment.Status = PaymentStatus.Pending;
            }

            payment.UpdatedAt = DateTime.UtcNow;
            // ✅ Không tự động save - để UnitOfWork quản lý
            return true;
        }

        public async Task UpdatePaymentAsync(Payment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask;
        }

        public async Task<Payment> ProcessPaymentAsync(Guid orderId, string method)
        {
            // Parse phương thức thanh toán (COD, Wallet, Bank)
            var parsedMethod = Enum.TryParse<PaymentMethod>(method, true, out var paymentMethod)
                ? paymentMethod
                : PaymentMethod.COD;

            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                OrderId = orderId,
                Method = parsedMethod,
                Status = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Payments.AddAsync(payment);
            // ✅ Không tự động save - để UnitOfWork quản lý

            return payment;
        }

        public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        public async Task<Payment?> GetByIdAsync(Guid paymentId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<Payment?> GetByOrderCodeAsync(int orderCode)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        }

        public async Task AddHistoryAsync(PaymentHistory history)
        {
            await _context.PaymentHistories.AddAsync(history);
            // ✅ Không tự động save - để UnitOfWork quản lý
        }

        public async Task<IEnumerable<PaymentHistory>> GetHistoryByPaymentIdAsync(Guid paymentId)
        {
            return await _context.PaymentHistories
                .Where(ph => ph.PaymentId == paymentId)
                .OrderByDescending(ph => ph.CreatedAt)
                .ToListAsync();
        }

        // ✅ New: Generate unique OrderCode để tránh collision
        public async Task<int> GenerateUniqueOrderCodeAsync()
        {
            // Strategy: Increment-based - lấy max OrderCode + 1
            // PayOS yêu cầu OrderCode là int (max 2,147,483,647)
            // Bắt đầu từ 1000000 để tránh conflict với các số nhỏ
            
            var maxOrderCode = await _context.Payments
                .Where(p => p.OrderCode.HasValue)
                .MaxAsync(p => (int?)p.OrderCode) ?? 999999; // Default to 999999 if no payments exist
            
            var orderCode = maxOrderCode + 1;
            
            // Ensure OrderCode is within int range
            if (orderCode > int.MaxValue - 1000)
            {
                // If approaching max, use timestamp-based approach
                var timestamp = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000000); // Last 9 digits
                var random = new Random().Next(1000, 9999);
                orderCode = timestamp * 10000 + random;
                
                // Ensure it's still valid
                if (orderCode > int.MaxValue)
                {
                    orderCode = orderCode % int.MaxValue;
                }
            }

            // Double-check uniqueness (should be rare)
            var exists = await _context.Payments
                .AnyAsync(p => p.OrderCode == orderCode);

            if (exists)
            {
                // Retry with timestamp + random
                var timestamp = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000000);
                var random = new Random().Next(10000, 99999);
                orderCode = timestamp * 100000 + random;
                
                if (orderCode > int.MaxValue)
                {
                    orderCode = orderCode % int.MaxValue;
                }
            }

            return orderCode;
        }

        // ✅ New: Statistics methods
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Payments.CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(PaymentStatus status)
        {
            return await _context.Payments.CountAsync(p => p.Status == status);
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));

            return await query.SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount)) ?? 0;
        }

        public async Task<Dictionary<PaymentStatus, int>> GetPaymentsByStatusAsync()
        {
            return await _context.Payments
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<List<(Guid OrderId, decimal Amount, DateTime CreatedAt)>> GetExpiredPaymentLinksAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Payments
                .Where(p => p.Status == PaymentStatus.Pending
                    && p.PaymentLinkExpiredAt.HasValue
                    && p.PaymentLinkExpiredAt.Value < now)
                .Select(p => new ValueTuple<Guid, decimal, DateTime>(
                    p.OrderId,
                    p.Amount,
                    p.CreatedAt))
                .ToListAsync();
        }

        // ✅ New: Aggregations per shop using paid-date
        public async Task<(decimal TotalRevenue, decimal Cash, decimal Cod, decimal Bank, decimal Wallet, int PaidCount)> GetShopPaidRevenueBreakdownAsync(
            Guid shopId,
            DateTime? from = null,
            DateTime? to = null,
            IEnumerable<PaymentMethod>? methods = null)
        {
            var paidHistories = _context.PaymentHistories
                .Where(ph => ph.NewStatus == PaymentStatus.Paid)
                .AsQueryable();

            var query = from ph in paidHistories
                        join p in _context.Payments on ph.PaymentId equals p.PaymentId
                        join o in _context.Orders on p.OrderId equals o.Id
                        where o.ShopId == shopId && p.Status == PaymentStatus.Paid
                        select new { p, ph.CreatedAt };

            if (from.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= from.Value);
            }
            if (to.HasValue)
            {
                var endInclusive = to.Value.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.CreatedAt <= endInclusive);
            }
            if (methods != null)
            {
                var methodList = methods.ToList();
                if (methodList.Count > 0)
                {
                    query = query.Where(x => methodList.Contains(x.p.Method));
                }
            }

            var byMethod = await query
                .GroupBy(x => x.p.Method)
                .Select(g => new
                {
                    Method = g.Key,
                    Revenue = g.Sum(x => x.p.Amount - x.p.RefundedAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            var totalRevenue = byMethod.Sum(x => x.Revenue);
            var paidCount = byMethod.Sum(x => x.Count);
            decimal cash = 0, cod = 0, bank = 0, wallet = 0;
            foreach (var m in byMethod)
            {
                switch (m.Method)
                {
                    case PaymentMethod.Cash: cash = m.Revenue; break;
                    case PaymentMethod.COD: cod = m.Revenue; break;
                    case PaymentMethod.Bank: bank = m.Revenue; break;
                    case PaymentMethod.Wallet: wallet = m.Revenue; break;
                }
            }

            return (totalRevenue, cash, cod, bank, wallet, paidCount);
        }

        public async Task<List<(DateTime BucketStart, decimal Revenue, int PaidCount)>> GetShopPaidRevenueTimeseriesAsync(
            Guid shopId,
            DateTime? from = null,
            DateTime? to = null,
            string groupBy = "day",
            IEnumerable<PaymentMethod>? methods = null)
        {
            var paidHistories = _context.PaymentHistories
                .Where(ph => ph.NewStatus == PaymentStatus.Paid)
                .AsQueryable();

            var query = from ph in paidHistories
                        join p in _context.Payments on ph.PaymentId equals p.PaymentId
                        join o in _context.Orders on p.OrderId equals o.Id
                        where o.ShopId == shopId && p.Status == PaymentStatus.Paid
                        select new { p, ph.CreatedAt };

            if (from.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= from.Value);
            }
            if (to.HasValue)
            {
                var endInclusive = to.Value.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.CreatedAt <= endInclusive);
            }
            if (methods != null)
            {
                var methodList = methods.ToList();
                if (methodList.Count > 0)
                {
                    query = query.Where(x => methodList.Contains(x.p.Method));
                }
            }

            if (string.Equals(groupBy, "week", StringComparison.OrdinalIgnoreCase))
            {
                var data = await query
                    .Select(x => new { Date = x.CreatedAt, Revenue = x.p.Amount - x.p.RefundedAmount })
                    .ToListAsync();

                DateTime StartOfWeek(DateTime dt)
                {
                    var d = dt.Date;
                    int diff = (int)d.DayOfWeek == 0 ? -6 : 1 - (int)d.DayOfWeek; // Monday as start
                    return d.AddDays(diff);
                }

                var grouped = data
                    .GroupBy(x => StartOfWeek(x.Date))
                    .Select(g => new ValueTuple<DateTime, decimal, int>(
                        g.Key,
                        g.Sum(x => x.Revenue),
                        g.Count()))
                    .OrderBy(x => x.Item1)
                    .ToList();

                return grouped;
            }
            else
            {
                // Materialize trước rồi group ở memory để tránh lỗi EF Core khi GroupBy theo Date
                var data = await query
                    .Select(x => new { Date = x.CreatedAt.Date, Revenue = x.p.Amount - x.p.RefundedAmount })
                    .ToListAsync();

                var grouped = data
                    .GroupBy(x => x.Date)
                    .Select(g => new ValueTuple<DateTime, decimal, int>(
                        g.Key,
                        g.Sum(x => x.Revenue),
                        g.Count()))
                    .OrderBy(x => x.Item1)
                    .ToList();

                return grouped;
            }
        }
        
        // ✅ New: Add payment with transaction support
        public async Task<Payment> AddWithTransactionAsync(Payment payment, Func<Task>? beforeSave = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Payments.AddAsync(payment);
                
                if (beforeSave != null)
                {
                    await beforeSave();
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return payment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        
        // ✅ New: Update payment with transaction support
        public async Task<int> UpdatePaymentWithTransactionAsync(Payment payment, Func<Task>? beforeSave = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                payment.UpdatedAt = DateTime.UtcNow;
                _context.Payments.Update(payment);
                
                if (beforeSave != null)
                {
                    await beforeSave();
                }
                
                int result = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
