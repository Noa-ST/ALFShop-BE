using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ServiceResponse<PayOSCreatePaymentResponse>> ProcessPaymentAsync(Guid orderId, string method);
        Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, string newStatus, string? changedBy = null, string? reason = null);
        Task<ServiceResponse<bool>> ProcessWebhookAsync(PayOSWebhookRequest webhook);
        Task<ServiceResponse<bool>> RefundAsync(RefundRequest request);
        Task<ServiceResponse<List<PaymentHistory>>> GetPaymentHistoryAsync(Guid paymentId);
    }
}
