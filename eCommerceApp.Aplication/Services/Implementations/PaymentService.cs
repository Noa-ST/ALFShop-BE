using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Application.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Repositories;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<ServiceResponse<bool>> ProcessPaymentAsync(Guid orderId, string method)
        {
            try
            {
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    OrderId = orderId,
                    Method = Enum.TryParse(method, true, out PaymentMethod parsedMethod)
                        ? parsedMethod
                        : PaymentMethod.Cash,
                    Status = PaymentStatus.Paid,
                    CreatedAt = DateTime.UtcNow
                };

                await _paymentRepository.AddAsync(payment);
                return ServiceResponse<bool>.Success(true, "Thanh toán thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail($"Lỗi khi xử lý thanh toán: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, string newStatus)
        {
            try
            {
                var success = await _paymentRepository.UpdateStatusAsync(paymentId, newStatus);

                return success
                    ? ServiceResponse<bool>.Success(true, "Cập nhật trạng thái thành công.")
                    : ServiceResponse<bool>.Fail("Không tìm thấy payment để cập nhật.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail($"Lỗi khi cập nhật trạng thái: {ex.Message}");
            }
        }
    }
}
