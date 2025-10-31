using eCommerceApp.Aplication.DTOs;

namespace eCommerceApp.Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ServiceResponse<bool>> ProcessPaymentAsync(Guid orderId, string method);
        Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, string newStatus);
    }
}
