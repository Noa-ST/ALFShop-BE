using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Application.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using System.Net;
using Microsoft.Extensions.Configuration;
using PaymentHistory = eCommerceApp.Domain.Entities.PaymentHistory;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPayOSService _payOSService;
        private readonly IConfiguration _configuration;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IPayOSService payOSService,
            IConfiguration configuration)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _payOSService = payOSService;
            _configuration = configuration;
        }

        public async Task<ServiceResponse<PayOSCreatePaymentResponse>> ProcessPaymentAsync(Guid orderId, string method)
        {
            try
            {
                // 1. Validate Order exists
                Order order;
                try
                {
                    order = await _orderRepository.GetByIdAsync(orderId);
                }
                catch (Exception)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không tìm thấy đơn hàng.",
                        HttpStatusCode.NotFound);
                }

                // 2. Validate Order status
                if (order.Status == OrderStatus.Canceled)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không thể thanh toán cho đơn hàng đã bị hủy.",
                        HttpStatusCode.BadRequest);
                }

                // 3. Validate duplicate payment
                var existingPayment = await _paymentRepository.GetByOrderIdAsync(orderId);
                if (existingPayment != null)
                {
                    if (existingPayment.Status == PaymentStatus.Paid)
                    {
                        return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                            "Đơn hàng này đã được thanh toán.",
                            HttpStatusCode.BadRequest);
                    }
                    // Nếu payment đã tồn tại nhưng chưa paid, có thể tiếp tục
                }

                // 4. Parse PaymentMethod
                if (!Enum.TryParse<PaymentMethod>(method, true, out var paymentMethod))
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        $"Phương thức thanh toán không hợp lệ: {method}",
                        HttpStatusCode.BadRequest);
                }

                // 5. Xử lý theo phương thức thanh toán
                if (paymentMethod == PaymentMethod.COD || paymentMethod == PaymentMethod.Cash)
                {
                    // COD/Cash: Tạo payment trực tiếp, không cần PayOS
                    var payment = existingPayment ?? new Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        OrderId = orderId,
                        Method = paymentMethod,
                        Status = PaymentStatus.Paid,
                        Amount = order.TotalAmount,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (existingPayment == null)
                    {
                        await _paymentRepository.AddAsync(payment);
                    }
                    else
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.Method = paymentMethod;
                        payment.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepository.UpdateStatusAsync(payment.PaymentId, PaymentStatus.Paid.ToString());
                    }

                    // Lưu history
                    await _paymentRepository.AddHistoryAsync(new PaymentHistory
                    {
                        HistoryId = Guid.NewGuid(),
                        PaymentId = payment.PaymentId,
                        OldStatus = existingPayment?.Status ?? PaymentStatus.Pending,
                        NewStatus = PaymentStatus.Paid,
                        Reason = $"Thanh toán {paymentMethod}",
                        ChangedBy = "System",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Update Order
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaymentMethod = paymentMethod;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateOrderAsync(order);

                    return ServiceResponse<PayOSCreatePaymentResponse>.Success(
                        new PayOSCreatePaymentResponse { Code = 0, Desc = "Thanh toán COD thành công." },
                        "Thanh toán thành công.");
                }
                else
                {
                    // Online payment (Wallet, Bank): Tạo payment link từ PayOS
                    var orderCode = orderId.GetHashCode(); // Convert Guid to int (PayOS yêu cầu int)
                    if (orderCode < 0) orderCode = Math.Abs(orderCode);

                    var frontendUrl = _configuration["PayOS:FrontendUrl"] ?? "http://localhost:3000";
                    var payOSRequest = new PayOSCreatePaymentRequest
                    {
                        OrderCode = orderCode,
                        Amount = (int)(order.TotalAmount * 100), // Convert to VND (cents)
                        Description = $"Thanh toán đơn hàng #{orderId}",
                        Items = new List<PayOSItem>
                        {
                            new PayOSItem
                            {
                                Name = $"Đơn hàng {orderId}",
                                Quantity = 1,
                                Price = (int)(order.TotalAmount * 100)
                            }
                        },
                        CancelUrl = $"{frontendUrl}/payment/cancel?orderId={orderId}",
                        ReturnUrl = $"{frontendUrl}/payment/return?orderId={orderId}",
                        ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
                    };

                    var payOSResponse = await _payOSService.CreatePaymentLinkAsync(payOSRequest);

                    if (payOSResponse.Code != 0 || payOSResponse.Data == null)
                    {
                        return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                            $"Lỗi khi tạo payment link: {payOSResponse.Desc}",
                            HttpStatusCode.BadRequest);
                    }

                    // Tạo hoặc cập nhật Payment record
                    var payment = existingPayment ?? new Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        OrderId = orderId,
                        Method = paymentMethod,
                        Status = PaymentStatus.Pending,
                        Amount = order.TotalAmount,
                        TransactionId = payOSResponse.Data.PaymentLinkId,
                        OrderCode = orderCode, // Lưu OrderCode để map với webhook
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (existingPayment == null)
                    {
                        await _paymentRepository.AddAsync(payment);
                    }
                    else
                    {
                        // Cập nhật payment nếu đã tồn tại
                        existingPayment.TransactionId = payOSResponse.Data.PaymentLinkId;
                        existingPayment.OrderCode = orderCode;
                        existingPayment.Status = PaymentStatus.Pending;
                        existingPayment.Method = paymentMethod;
                        existingPayment.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepository.UpdateStatusAsync(existingPayment.PaymentId, PaymentStatus.Pending.ToString());
                        payment = existingPayment;
                    }

                    // Lưu history
                    await _paymentRepository.AddHistoryAsync(new PaymentHistory
                    {
                        HistoryId = Guid.NewGuid(),
                        PaymentId = payment.PaymentId,
                        OldStatus = existingPayment?.Status ?? PaymentStatus.Pending,
                        NewStatus = PaymentStatus.Pending,
                        Reason = "Tạo payment link từ PayOS",
                        ChangedBy = "System",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Update Order (chưa paid, đang pending)
                    order.PaymentStatus = PaymentStatus.Pending;
                    order.PaymentMethod = paymentMethod;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateOrderAsync(order);

                    return ServiceResponse<PayOSCreatePaymentResponse>.Success(
                        payOSResponse,
                        "Tạo payment link thành công. Vui lòng thanh toán.");
                }
            }
            catch (Exception ex)
            {
                return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                    $"Lỗi khi xử lý thanh toán: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(
            Guid paymentId,
            string newStatus,
            string? changedBy = null,
            string? reason = null)
        {
            try
            {
                // 1. Get payment
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy payment để cập nhật.",
                        HttpStatusCode.NotFound);
                }

                // 2. Validate status
                if (!Enum.TryParse<PaymentStatus>(newStatus, true, out var parsedStatus))
                {
                    return ServiceResponse<bool>.Fail(
                        $"Trạng thái thanh toán không hợp lệ: {newStatus}",
                        HttpStatusCode.BadRequest);
                }

                var oldStatus = payment.Status;

                // 3. Update payment status
                var success = await _paymentRepository.UpdateStatusAsync(paymentId, newStatus);
                if (!success)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể cập nhật trạng thái payment.",
                        HttpStatusCode.InternalServerError);
                }

                // 4. Lưu history
                await _paymentRepository.AddHistoryAsync(new PaymentHistory
                {
                    HistoryId = Guid.NewGuid(),
                    PaymentId = paymentId,
                    OldStatus = oldStatus,
                    NewStatus = parsedStatus,
                    Reason = reason ?? "Cập nhật thủ công",
                    ChangedBy = changedBy ?? "System",
                    CreatedAt = DateTime.UtcNow
                });

                // 5. Đồng bộ Order.PaymentStatus
                try
                {
                    var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                    if (order != null)
                    {
                        order.PaymentStatus = parsedStatus;
                        order.UpdatedAt = DateTime.UtcNow;
                        await _orderRepository.UpdateOrderAsync(order);
                    }
                }
                catch (Exception ex)
                {
                    // Log warning nhưng không fail toàn bộ operation
                    // TODO: Log warning
                }

                return ServiceResponse<bool>.Success(true, "Cập nhật trạng thái thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi cập nhật trạng thái: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> ProcessWebhookAsync(PayOSWebhookRequest webhook)
        {
            try
            {
                // 1. Verify signature
                var checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;
                var isValid = await _payOSService.VerifyWebhookSignatureAsync(webhook, checksumKey);
                if (!isValid)
                {
                    return ServiceResponse<bool>.Fail(
                        "Webhook signature không hợp lệ.",
                        HttpStatusCode.Unauthorized);
                }

                // 2. Validate webhook data
                if (webhook.Data == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Webhook data không hợp lệ.",
                        HttpStatusCode.BadRequest);
                }

                // 3. Tìm payment bằng OrderCode từ webhook
                var payment = await _paymentRepository.GetByOrderCodeAsync(webhook.Data.OrderCode);
                if (payment == null)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không tìm thấy payment với OrderCode: {webhook.Data.OrderCode}",
                        HttpStatusCode.NotFound);
                }

                // 4. Determine payment status từ PayOS code
                // "00" = thành công, các mã khác = thất bại
                PaymentStatus newStatus = webhook.Data.Code == "00" 
                    ? PaymentStatus.Paid 
                    : PaymentStatus.Failed;

                var oldStatus = payment.Status;

                // 5. Chỉ cập nhật nếu status thay đổi
                if (oldStatus != newStatus)
                {
                    // 6. Update payment status
                    await _paymentRepository.UpdateStatusAsync(payment.PaymentId, newStatus.ToString());

                    // 7. Cập nhật TransactionId nếu có từ webhook
                    if (!string.IsNullOrEmpty(webhook.Data.Reference) && payment.TransactionId != webhook.Data.Reference)
                    {
                        payment.TransactionId = webhook.Data.Reference;
                        payment.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepository.UpdatePaymentAsync(payment);
                    }

                    // 8. Lưu history
                    await _paymentRepository.AddHistoryAsync(new PaymentHistory
                    {
                        HistoryId = Guid.NewGuid(),
                        PaymentId = payment.PaymentId,
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        Reason = $"Webhook từ PayOS: {webhook.Data.Desc}",
                        ChangedBy = "PayOS",
                        CreatedAt = DateTime.UtcNow
                    });

                    // 9. Đồng bộ Order.PaymentStatus
                    try
                    {
                        var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                        if (order != null)
                        {
                            order.PaymentStatus = newStatus;
                            order.UpdatedAt = DateTime.UtcNow;
                            
                            // Nếu thanh toán thành công, cập nhật TransactionId vào Order nếu cần
                            if (newStatus == PaymentStatus.Paid && !string.IsNullOrEmpty(webhook.Data.Reference))
                            {
                                // Có thể lưu vào Order nếu có field TransactionId
                            }
                            
                            await _orderRepository.UpdateOrderAsync(order);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log warning nhưng không fail toàn bộ operation
                        // TODO: Log warning - Không thể cập nhật Order
                    }
                }

                return ServiceResponse<bool>.Success(true, "Webhook processed successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi xử lý webhook: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> RefundAsync(RefundRequest request)
        {
            try
            {
                // 1. Get payment
                var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
                if (payment == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy payment.",
                        HttpStatusCode.NotFound);
                }

                // 2. Validate payment status
                if (payment.Status != PaymentStatus.Paid)
                {
                    return ServiceResponse<bool>.Fail(
                        "Chỉ có thể hoàn tiền cho payment đã thanh toán.",
                        HttpStatusCode.BadRequest);
                }

                // 3. Validate amount
                if (request.Amount <= 0 || request.Amount > payment.Amount)
                {
                    return ServiceResponse<bool>.Fail(
                        "Số tiền hoàn không hợp lệ.",
                        HttpStatusCode.BadRequest);
                }

                // 4. Process refund với PayOS (nếu là online payment)
                if (payment.Method == PaymentMethod.Wallet || payment.Method == PaymentMethod.Bank)
                {
                    if (string.IsNullOrEmpty(payment.TransactionId))
                    {
                        return ServiceResponse<bool>.Fail(
                            "Không tìm thấy TransactionId để hoàn tiền.",
                            HttpStatusCode.BadRequest);
                    }

                    // Convert Guid to int OrderCode (tạm thời)
                    var orderCode = payment.OrderId.GetHashCode();
                    if (orderCode < 0) orderCode = Math.Abs(orderCode);

                    var refundAmount = (int)(request.Amount * 100); // Convert to cents
                    var success = await _payOSService.RefundAsync(orderCode, refundAmount, request.Reason);
                    
                    if (!success)
                    {
                        return ServiceResponse<bool>.Fail(
                            "Lỗi khi xử lý hoàn tiền với PayOS.",
                            HttpStatusCode.InternalServerError);
                    }
                }

                // 5. Update payment status
                var newStatus = request.Amount == payment.Amount 
                    ? PaymentStatus.Failed // Full refund
                    : PaymentStatus.Paid; // Partial refund, vẫn giữ Paid

                if (newStatus == PaymentStatus.Failed)
                {
                    await _paymentRepository.UpdateStatusAsync(payment.PaymentId, PaymentStatus.Failed.ToString());
                }

                // 6. Lưu history
                await _paymentRepository.AddHistoryAsync(new PaymentHistory
                {
                    HistoryId = Guid.NewGuid(),
                    PaymentId = request.PaymentId,
                    OldStatus = payment.Status,
                    NewStatus = newStatus,
                    Reason = $"Hoàn tiền: {request.Reason}",
                    ChangedBy = "System",
                    CreatedAt = DateTime.UtcNow
                });

                // 7. Update Order
                try
                {
                    var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                    if (order != null && newStatus == PaymentStatus.Failed)
                    {
                        order.PaymentStatus = PaymentStatus.Failed;
                        order.UpdatedAt = DateTime.UtcNow;
                        await _orderRepository.UpdateOrderAsync(order);
                    }
                }
                catch
                {
                    // Log warning
                }

                return ServiceResponse<bool>.Success(true, "Hoàn tiền thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi hoàn tiền: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<List<PaymentHistory>>> GetPaymentHistoryAsync(Guid paymentId)
        {
            try
            {
                var history = await _paymentRepository.GetHistoryByPaymentIdAsync(paymentId);
                return ServiceResponse<List<PaymentHistory>>.Success(
                    history.ToList(),
                    "Lấy lịch sử thanh toán thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<PaymentHistory>>.Fail(
                    $"Lỗi khi lấy lịch sử: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }
    }
}
