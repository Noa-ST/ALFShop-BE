using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<bool> UpdateStatusAsync(Guid paymentId, string newStatus);
        Task UpdatePaymentAsync(Payment payment);
        Task<Payment> ProcessPaymentAsync(Guid orderId, string method);
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
        Task<Payment?> GetByIdAsync(Guid paymentId);
        Task<Payment?> GetByOrderCodeAsync(int orderCode);
        Task AddHistoryAsync(PaymentHistory history);
        Task<IEnumerable<PaymentHistory>> GetHistoryByPaymentIdAsync(Guid paymentId);
    }
}
