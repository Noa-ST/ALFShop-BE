using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
    }
}
