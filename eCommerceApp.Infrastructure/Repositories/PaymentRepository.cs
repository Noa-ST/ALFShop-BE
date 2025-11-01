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
            await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdatePaymentAsync(Payment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();

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
            await _context.SaveChangesAsync();
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
    }
}
