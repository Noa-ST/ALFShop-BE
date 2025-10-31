using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<bool> UpdateStatusAsync(Guid paymentId, string newStatus);
        Task<Payment> ProcessPaymentAsync(Guid orderId, string method);
    }
}
