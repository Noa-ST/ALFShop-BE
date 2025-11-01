using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ServiceResponse<PayOSCreatePaymentResponse>> ProcessPaymentAsync(Guid orderId, string method, string userId);
        Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, string newStatus, string userId, string? changedBy = null, string? reason = null);
        Task<ServiceResponse<bool>> ProcessWebhookAsync(PayOSWebhookRequest webhook);
        Task<ServiceResponse<bool>> RefundAsync(RefundRequest request, string userId);
        Task<ServiceResponse<List<PaymentHistory>>> GetPaymentHistoryAsync(Guid paymentId, string userId);
        
        // ✅ New: Get Payment by OrderId
        Task<ServiceResponse<Payment>> GetPaymentByOrderIdAsync(Guid orderId, string userId);
        
        // ✅ New: Cancel Payment Link
        Task<ServiceResponse<bool>> CancelPaymentLinkAsync(Guid paymentId, string userId);
        
        // ✅ New: Retry Failed Payment
        Task<ServiceResponse<PayOSCreatePaymentResponse>> RetryPaymentAsync(Guid paymentId, string userId);
        
        // ✅ New: Payment Statistics
        Task<ServiceResponse<object>> GetPaymentStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        // ✅ New: Expire Payment Links (background job)
        Task<ServiceResponse<int>> ExpirePaymentLinksAsync();
    }
}
