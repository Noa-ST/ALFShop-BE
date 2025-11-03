using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Domain.Interfaces;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using PaymentHistory = eCommerceApp.Domain.Entities.PaymentHistory;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IPayOSService _payOSService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppLogger<PaymentService> _logger; // ✅ New: Structured logging

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IShopRepository shopRepository,
            IPayOSService payOSService,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IAppLogger<PaymentService> logger) // ✅ New: Inject IAppLogger
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _shopRepository = shopRepository;
            _payOSService = payOSService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ✅ Helper method để check admin role
        private bool IsAdmin(string? userId = null)
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
        }

        public async Task<ServiceResponse<PayOSCreatePaymentResponse>> ProcessPaymentAsync(Guid orderId, string method, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                // 1. ✅ Fix: Validate Order exists (check null instead of catch Exception)
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không tìm thấy đơn hàng.",
                        HttpStatusCode.NotFound);
                }

                // ✅ New: Validate Order amount > 0
                if (order.TotalAmount <= 0)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Tổng tiền đơn hàng phải lớn hơn 0.",
                        HttpStatusCode.BadRequest);
                }

                // ✅ New: Validate Order status - không thể thanh toán order đã Delivered
                if (order.Status == OrderStatus.Delivered)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không thể thanh toán cho đơn hàng đã được giao.",
                        HttpStatusCode.BadRequest);
                }

                // ✅ New: Validate ownership - Customer chỉ có thể thanh toán order của mình, Admin có thể thanh toán tất cả
                var isAdmin = IsAdmin(userId);
                if (!isAdmin && order.CustomerId != userId)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Bạn không có quyền thanh toán đơn hàng này.",
                        HttpStatusCode.Forbidden);
                }

                // 2. Validate Order status
                if (order.Status == OrderStatus.Canceled)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không thể thanh toán cho đơn hàng đã bị hủy.",
                        HttpStatusCode.BadRequest);
                }

                // 3. ✅ Fix: Check duplicate payment
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
                        Amount = order.TotalAmount, // ✅ Validate: Amount = Order.TotalAmount
                        RefundedAmount = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (existingPayment == null)
                    {
                        await _paymentRepository.AddAsync(payment);
                    }
                    else
                    {
                        // ✅ Fix: Update Amount nếu order total thay đổi
                        existingPayment.Status = PaymentStatus.Paid;
                        existingPayment.Method = paymentMethod;
                        existingPayment.Amount = order.TotalAmount;
                        existingPayment.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepository.UpdatePaymentAsync(existingPayment);
                        payment = existingPayment;
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

                // ✅ Note: Transaction được quản lý bởi UnitOfWork hoặc repository layer
                // Các repository methods không tự động save để UnitOfWork quản lý

                _logger.LogInformation($"COD payment processed successfully: OrderId={orderId}, PaymentId={payment.PaymentId}, Amount={payment.Amount}");

                return ServiceResponse<PayOSCreatePaymentResponse>.Success(
                        new PayOSCreatePaymentResponse { Code = 0, Desc = "Thanh toán COD thành công." },
                        "Thanh toán thành công.");
                }
                else
                {
                    // Online payment (Wallet, Bank): Tạo payment link từ PayOS
                    // ✅ Fix: Sử dụng GenerateUniqueOrderCodeAsync thay vì GetHashCode
                    var orderCode = await _paymentRepository.GenerateUniqueOrderCodeAsync();

                    var frontendUrl = _configuration["PayOS:FrontendUrl"] ?? "http://localhost:3000";
                    var expiredAt = DateTimeOffset.UtcNow.AddMinutes(15);
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
                        ExpiredAt = expiredAt.ToUnixTimeSeconds()
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
                        Amount = order.TotalAmount, // ✅ Validate: Amount = Order.TotalAmount
                        RefundedAmount = 0,
                        TransactionId = payOSResponse.Data.PaymentLinkId,
                        OrderCode = orderCode, // ✅ Fix: Unique OrderCode
                        PaymentLinkExpiredAt = expiredAt.DateTime, // ✅ New: Track expiry
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (existingPayment == null)
                    {
                        await _paymentRepository.AddAsync(payment);
                    }
                    else
                    {
                        // ✅ Fix: Update Amount và các fields khác
                        existingPayment.TransactionId = payOSResponse.Data.PaymentLinkId;
                        existingPayment.OrderCode = orderCode;
                        existingPayment.Status = PaymentStatus.Pending;
                        existingPayment.Method = paymentMethod;
                        existingPayment.Amount = order.TotalAmount; // Update amount
                        existingPayment.PaymentLinkExpiredAt = expiredAt.DateTime;
                        existingPayment.UpdatedAt = DateTime.UtcNow;
                        await _paymentRepository.UpdatePaymentAsync(existingPayment);
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

                    // ✅ Note: Transaction được quản lý bởi UnitOfWork hoặc repository layer

                    _logger.LogInformation($"Payment link created: OrderId={orderId}, PaymentId={payment.PaymentId}, OrderCode={orderCode}, Amount={payment.Amount}, ExpiredAt={expiredAt}");

                    return ServiceResponse<PayOSCreatePaymentResponse>.Success(
                        payOSResponse,
                        "Tạo payment link thành công. Vui lòng thanh toán.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process payment: OrderId={orderId}, Method={method}, UserId={userId}, Error={ex.Message}");
                return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                    $"Lỗi khi xử lý thanh toán: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(
            Guid paymentId,
            string newStatus,
            string userId,
            string? changedBy = null,
            string? reason = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                // ✅ New: Chỉ Admin mới có thể update payment status thủ công
                var isAdmin = IsAdmin(userId);
                if (!isAdmin)
                {
                    return ServiceResponse<bool>.Fail(
                        "Chỉ Admin mới có thể cập nhật trạng thái thanh toán.",
                        HttpStatusCode.Forbidden);
                }

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
                catch
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

                // ✅ Fix: Validate amount từ webhook - phải khớp với payment amount
                var webhookAmountInVND = webhook.Data.Amount / 100.0m; // Convert từ cents về VND
                if (Math.Abs(webhookAmountInVND - payment.Amount) > 0.01m) // Cho phép sai số 0.01 VND do rounding
                {
                    return ServiceResponse<bool>.Fail(
                        $"Amount không khớp. Payment Amount: {payment.Amount}, Webhook Amount: {webhookAmountInVND}",
                        HttpStatusCode.BadRequest);
                }

                // ✅ New: Validate payment link expiry (nếu có)
                if (payment.PaymentLinkExpiredAt.HasValue && payment.PaymentLinkExpiredAt.Value < DateTime.UtcNow)
                {
                    // Payment link đã hết hạn, nhưng vẫn có thể nhận webhook từ PayOS
                    // Log warning nhưng vẫn xử lý
                }

                // 4. Determine payment status từ PayOS code
                // "00" = thành công, các mã khác = thất bại
                PaymentStatus newStatus = webhook.Data.Code == "00" 
                    ? PaymentStatus.Paid 
                    : PaymentStatus.Failed;

                var oldStatus = payment.Status;

                // ✅ New: Idempotency check - kiểm tra xem đã có history với status này chưa
                var recentHistory = await _paymentRepository.GetHistoryByPaymentIdAsync(payment.PaymentId);
                var hasRecentStatusChange = recentHistory
                    .OrderByDescending(h => h.CreatedAt)
                    .FirstOrDefault(h => h.NewStatus == newStatus && (h.Reason?.Contains("Webhook từ PayOS") ?? false));

                // 5. Chỉ cập nhật nếu status thay đổi và chưa được xử lý
                if (oldStatus != newStatus && hasRecentStatusChange == null)
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
                }
                else if (hasRecentStatusChange != null)
                {
                    // Duplicate webhook - đã xử lý rồi, trả về success
                    return ServiceResponse<bool>.Success(true, "Webhook đã được xử lý trước đó.");
                }

                // 9. Đồng bộ Order.PaymentStatus (chỉ khi status thay đổi)
                if (oldStatus != newStatus)
                {
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
                    catch
                    {
                        // Log warning nhưng không fail toàn bộ operation
                        // TODO: Log warning - Không thể cập nhật Order
                    }
                }

                _logger.LogInformation($"Webhook processed successfully: OrderCode={webhook.Data.OrderCode}, PaymentId={payment.PaymentId}, Status={newStatus}");
                return ServiceResponse<bool>.Success(true, "Webhook processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process webhook: OrderCode={webhook.Data?.OrderCode}, Error={ex.Message}");
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi xử lý webhook: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> RefundAsync(RefundRequest request, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                // 1. Get payment
                var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
                if (payment == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy payment.",
                        HttpStatusCode.NotFound);
                }

                // ✅ New: Validate ownership - Seller chỉ có thể refund payment của shop mình, Admin có thể refund tất cả
                var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                if (order == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy đơn hàng liên quan.",
                        HttpStatusCode.NotFound);
                }

                var isAdmin = IsAdmin(userId);
                var shop = await _shopRepository.GetByIdAsync(order.ShopId);
                var isShopOwner = shop != null && shop.SellerId == userId;

                if (!isAdmin && !isShopOwner)
                {
                    return ServiceResponse<bool>.Fail(
                        "Bạn không có quyền hoàn tiền cho payment này.",
                        HttpStatusCode.Forbidden);
                }

                // ✅ New: Validate Order status - không thể refund order đã delivered hoặc canceled
                if (order.Status == OrderStatus.Delivered)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể hoàn tiền cho đơn hàng đã được giao thành công. Vui lòng liên hệ hỗ trợ.",
                        HttpStatusCode.BadRequest);
                }

                if (order.Status == OrderStatus.Canceled)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể hoàn tiền cho đơn hàng đã bị hủy.",
                        HttpStatusCode.BadRequest);
                }

                // 2. Validate payment status
                if (payment.Status != PaymentStatus.Paid)
                {
                    return ServiceResponse<bool>.Fail(
                        "Chỉ có thể hoàn tiền cho payment đã thanh toán.",
                        HttpStatusCode.BadRequest);
                }

                // ✅ Fix: Validate amount - không thể refund quá số tiền còn lại
                var remainingAmount = payment.Amount - payment.RefundedAmount;
                if (request.Amount <= 0 || request.Amount > remainingAmount)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Số tiền hoàn không hợp lệ. Số tiền còn lại có thể hoàn: {remainingAmount}",
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

                    if (!payment.OrderCode.HasValue)
                    {
                        return ServiceResponse<bool>.Fail(
                            "Không tìm thấy OrderCode để hoàn tiền.",
                            HttpStatusCode.BadRequest);
                    }

                    // ✅ Fix: Sử dụng OrderCode từ payment thay vì GetHashCode
                    var orderCode = payment.OrderCode.Value;
                    var refundAmount = (int)(request.Amount * 100); // Convert to cents
                    var success = await _payOSService.RefundAsync(orderCode, refundAmount, request.Reason);
                    
                    if (!success)
                    {
                        return ServiceResponse<bool>.Fail(
                            "Lỗi khi xử lý hoàn tiền với PayOS.",
                            HttpStatusCode.InternalServerError);
                    }
                }

                // ✅ Fix: Save old status và refunded amount trước khi update
                var oldStatusForHistory = payment.Status;
                var oldRefundedAmount = payment.RefundedAmount;

                // Update RefundedAmount
                payment.RefundedAmount += request.Amount;
                payment.UpdatedAt = DateTime.UtcNow;

                // 5. Update payment status chỉ khi full refund
                var newStatus = payment.RefundedAmount >= payment.Amount 
                    ? PaymentStatus.Failed // Full refund
                    : PaymentStatus.Paid; // Partial refund, vẫn giữ Paid

                if (newStatus == PaymentStatus.Failed)
                {
                    payment.Status = PaymentStatus.Failed;
                }

                await _paymentRepository.UpdatePaymentAsync(payment);

                // 6. Lưu history
                await _paymentRepository.AddHistoryAsync(new PaymentHistory
                {
                    HistoryId = Guid.NewGuid(),
                    PaymentId = request.PaymentId,
                    OldStatus = oldStatusForHistory,
                    NewStatus = newStatus,
                    Reason = $"Hoàn tiền {request.Amount} VND: {request.Reason}. Tổng đã hoàn: {payment.RefundedAmount} VND (trước đó: {oldRefundedAmount} VND)",
                    ChangedBy = isAdmin ? "Admin" : "Seller",
                    CreatedAt = DateTime.UtcNow
                });

                // 7. Update Order
                try
                {
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

        public async Task<ServiceResponse<List<PaymentHistory>>> GetPaymentHistoryAsync(Guid paymentId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<List<PaymentHistory>>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                // ✅ New: Validate ownership
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return ServiceResponse<List<PaymentHistory>>.Fail(
                        "Không tìm thấy payment.",
                        HttpStatusCode.NotFound);
                }

                var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                if (order == null)
                {
                    return ServiceResponse<List<PaymentHistory>>.Fail(
                        "Không tìm thấy đơn hàng liên quan.",
                        HttpStatusCode.NotFound);
                }

                var isAdmin = IsAdmin(userId);
                var isCustomer = order.CustomerId == userId;
                var shop = await _shopRepository.GetByIdAsync(order.ShopId);
                var isShopOwner = shop != null && shop.SellerId == userId;

                if (!isAdmin && !isCustomer && !isShopOwner)
                {
                    return ServiceResponse<List<PaymentHistory>>.Fail(
                        "Bạn không có quyền xem lịch sử thanh toán này.",
                        HttpStatusCode.Forbidden);
                }

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

        // ✅ New: Get Payment by OrderId
        public async Task<ServiceResponse<Payment>> GetPaymentByOrderIdAsync(Guid orderId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<Payment>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
                if (payment == null)
                {
                    return ServiceResponse<Payment>.Fail(
                        "Không tìm thấy payment cho đơn hàng này.",
                        HttpStatusCode.NotFound);
                }

                // Validate ownership
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return ServiceResponse<Payment>.Fail(
                        "Không tìm thấy đơn hàng.",
                        HttpStatusCode.NotFound);
                }

                var isAdmin = IsAdmin(userId);
                var isCustomer = order.CustomerId == userId;
                var shop = await _shopRepository.GetByIdAsync(order.ShopId);
                var isShopOwner = shop != null && shop.SellerId == userId;

                if (!isAdmin && !isCustomer && !isShopOwner)
                {
                    return ServiceResponse<Payment>.Fail(
                        "Bạn không có quyền xem payment này.",
                        HttpStatusCode.Forbidden);
                }

                return ServiceResponse<Payment>.Success(payment, "Lấy thông tin payment thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<Payment>.Fail(
                    $"Lỗi khi lấy payment: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Cancel Payment Link
        public async Task<ServiceResponse<bool>> CancelPaymentLinkAsync(Guid paymentId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy payment.",
                        HttpStatusCode.NotFound);
                }

                // Validate ownership
                var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                if (order == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy đơn hàng liên quan.",
                        HttpStatusCode.NotFound);
                }

                var isAdmin = IsAdmin(userId);
                var isCustomer = order.CustomerId == userId;
                var shop = await _shopRepository.GetByIdAsync(order.ShopId);
                var isShopOwner = shop != null && shop.SellerId == userId;

                if (!isAdmin && !isCustomer && !isShopOwner)
                {
                    return ServiceResponse<bool>.Fail(
                        "Bạn không có quyền hủy payment link này.",
                        HttpStatusCode.Forbidden);
                }

                // Chỉ có thể cancel payment link chưa thanh toán
                if (payment.Status != PaymentStatus.Pending)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể hủy payment link ở trạng thái {payment.Status}. Chỉ có thể hủy payment link ở trạng thái Pending.",
                        HttpStatusCode.BadRequest);
                }

                // Update status to Failed
                await _paymentRepository.UpdateStatusAsync(paymentId, PaymentStatus.Failed.ToString());

                // Lưu history
                await _paymentRepository.AddHistoryAsync(new PaymentHistory
                {
                    HistoryId = Guid.NewGuid(),
                    PaymentId = paymentId,
                    OldStatus = PaymentStatus.Pending,
                    NewStatus = PaymentStatus.Failed,
                    Reason = "Hủy payment link",
                    ChangedBy = isAdmin ? "Admin" : (isCustomer ? "Customer" : "Seller"),
                    CreatedAt = DateTime.UtcNow
                });

                // Update Order
                try
                {
                    order.PaymentStatus = PaymentStatus.Failed;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateOrderAsync(order);
                }
                catch
                {
                    // Log warning
                }

                return ServiceResponse<bool>.Success(true, "Hủy payment link thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi hủy payment link: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Retry Failed Payment
        public async Task<ServiceResponse<PayOSCreatePaymentResponse>> RetryPaymentAsync(Guid paymentId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không tìm thấy payment.",
                        HttpStatusCode.NotFound);
                }

                // Validate ownership
                var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                if (order == null)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không tìm thấy đơn hàng liên quan.",
                        HttpStatusCode.NotFound);
                }

                var isAdmin = IsAdmin(userId);
                if (!isAdmin && order.CustomerId != userId)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Bạn không có quyền retry payment này.",
                        HttpStatusCode.Forbidden);
                }

                // Chỉ có thể retry payment failed
                if (payment.Status != PaymentStatus.Failed)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        $"Không thể retry payment ở trạng thái {payment.Status}. Chỉ có thể retry payment ở trạng thái Failed.",
                        HttpStatusCode.BadRequest);
                }

                // Chỉ retry online payment
                if (payment.Method == PaymentMethod.COD || payment.Method == PaymentMethod.Cash)
                {
                    return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                        "Không thể retry payment COD/Cash. Vui lòng tạo payment mới.",
                        HttpStatusCode.BadRequest);
                }

                // Retry bằng cách tạo lại payment link
                return await ProcessPaymentAsync(order.Id, payment.Method.ToString(), userId);
            }
            catch (Exception ex)
            {
                return ServiceResponse<PayOSCreatePaymentResponse>.Fail(
                    $"Lỗi khi retry payment: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Payment Statistics
        public async Task<ServiceResponse<object>> GetPaymentStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var totalPayments = await _paymentRepository.GetTotalCountAsync();
                var paymentsByStatus = await _paymentRepository.GetPaymentsByStatusAsync();
                var totalRevenue = await _paymentRepository.GetTotalRevenueAsync(startDate, endDate);
                var paidCount = await _paymentRepository.GetCountByStatusAsync(PaymentStatus.Paid);
                var pendingCount = await _paymentRepository.GetCountByStatusAsync(PaymentStatus.Pending);
                var failedCount = await _paymentRepository.GetCountByStatusAsync(PaymentStatus.Failed);

                var statistics = new
                {
                    TotalPayments = totalPayments,
                    PaymentsByStatus = paymentsByStatus,
                    TotalRevenue = totalRevenue,
                    PaidCount = paidCount,
                    PendingCount = pendingCount,
                    FailedCount = failedCount,
                    DateRange = new
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    }
                };

                return ServiceResponse<object>.Success(statistics, "Lấy thống kê payment thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<object>.Fail(
                    $"Lỗi khi lấy thống kê: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Expire Payment Links
        public async Task<ServiceResponse<int>> ExpirePaymentLinksAsync()
        {
            try
            {
                var expiredPayments = await _paymentRepository.GetExpiredPaymentLinksAsync();
                int expiredCount = 0;

                foreach (var (orderId, amount, createdAt) in expiredPayments)
                {
                    var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
                    if (payment != null && payment.Status == PaymentStatus.Pending)
                    {
                        await _paymentRepository.UpdateStatusAsync(payment.PaymentId, PaymentStatus.Failed.ToString());
                        
                        // Lưu history
                        await _paymentRepository.AddHistoryAsync(new PaymentHistory
                        {
                            HistoryId = Guid.NewGuid(),
                            PaymentId = payment.PaymentId,
                            OldStatus = PaymentStatus.Pending,
                            NewStatus = PaymentStatus.Failed,
                            Reason = "Payment link đã hết hạn",
                            ChangedBy = "System",
                            CreatedAt = DateTime.UtcNow
                        });

                        // Update Order
                        try
                        {
                            var order = await _orderRepository.GetByIdAsync(orderId);
                            if (order != null)
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

                        expiredCount++;
                    }
                }

                if (expiredCount > 0)
                {
                    _logger.LogInformation($"Expired {expiredCount} payment links automatically");
                }
                return ServiceResponse<int>.Success(expiredCount, $"Đã expire {expiredCount} payment links.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to expire payment links: Error={ex.Message}");
                return ServiceResponse<int>.Fail(
                    $"Lỗi khi expire payment links: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }
    }
}
