using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;

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
        
        // ✅ New: Methods for unique OrderCode generation
        Task<int> GenerateUniqueOrderCodeAsync();
        
        // ✅ New: Methods for statistics and filtering
        Task<int> GetTotalCountAsync();
        Task<int> GetCountByStatusAsync(PaymentStatus status);
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<PaymentStatus, int>> GetPaymentsByStatusAsync();
        Task<List<(Guid OrderId, decimal Amount, DateTime CreatedAt)>> GetExpiredPaymentLinksAsync();
        
        // ✅ New: Transaction support methods
        Task<Payment> AddWithTransactionAsync(Payment payment, Func<Task>? beforeSave = null);
        Task<int> UpdatePaymentWithTransactionAsync(Payment payment, Func<Task>? beforeSave = null);
    }
}
