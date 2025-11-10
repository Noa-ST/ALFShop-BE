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

        // ✅ New: Aggregations per shop using paid-date (PaymentHistory.NewStatus == Paid)
        Task<(decimal TotalRevenue, decimal Cash, decimal Cod, decimal Bank, decimal Wallet, int PaidCount)> GetShopPaidRevenueBreakdownAsync(
            Guid shopId,
            DateTime? from = null,
            DateTime? to = null,
            IEnumerable<PaymentMethod>? methods = null);

        Task<List<(DateTime BucketStart, decimal Revenue, int PaidCount)>> GetShopPaidRevenueTimeseriesAsync(
            Guid shopId,
            DateTime? from = null,
            DateTime? to = null,
            string groupBy = "day",
            IEnumerable<PaymentMethod>? methods = null);
        
        // ✅ New: Transaction support methods
        Task<Payment> AddWithTransactionAsync(Payment payment, Func<Task>? beforeSave = null);
        Task<int> UpdatePaymentWithTransactionAsync(Payment payment, Func<Task>? beforeSave = null);
    }
}
