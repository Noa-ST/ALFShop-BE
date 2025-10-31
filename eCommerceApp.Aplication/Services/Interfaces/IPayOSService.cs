using eCommerceApp.Aplication.DTOs.Payment;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IPayOSService
    {
        Task<PayOSCreatePaymentResponse> CreatePaymentLinkAsync(PayOSCreatePaymentRequest request);
        Task<bool> VerifyWebhookSignatureAsync(PayOSWebhookRequest webhook, string checksumKey);
        Task<bool> RefundAsync(int orderCode, int amount, string reason);
    }
}

