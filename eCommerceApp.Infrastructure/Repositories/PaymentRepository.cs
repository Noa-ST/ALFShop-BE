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

            await _context.SaveChangesAsync();
            return true;
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
    }
}
