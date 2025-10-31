using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Application.DTOs.Order;
using eCommerceApp.Application.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Domain.Interfaces;
using System.Net;

namespace eCommerceApp.Application.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly IAddressRepository _addressRepo;
        private readonly IMapper _mapper;

        public OrderService(
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            IAddressRepository addressRepo,
            IMapper mapper)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _addressRepo = addressRepo;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> CreateOrderAsync(OrderCreateDTO dto)
        {
            try
            {
                // 1. Validate CustomerId
                if (string.IsNullOrEmpty(dto.CustomerId))
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        "CustomerId không được để trống.",
                        HttpStatusCode.BadRequest);
                }

                // 2. Validate Address
                var addresses = await _addressRepo.GetUserAddressesAsync(dto.CustomerId);
                var address = addresses.FirstOrDefault(a => a.Id == dto.AddressId);
                if (address == null)
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        "Địa chỉ giao hàng không hợp lệ.",
                        HttpStatusCode.BadRequest);
                }

                // 3. Validate Items và tính TotalAmount
                if (dto.Items == null || !dto.Items.Any())
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        "Đơn hàng phải có ít nhất một sản phẩm.",
                        HttpStatusCode.BadRequest);
                }

                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var itemDto in dto.Items)
                {
                    var product = await _productRepo.GetDetailByIdAsync(itemDto.ProductId);
                    if (product == null || product.IsDeleted)
                    {
                        return ServiceResponse<List<OrderResponseDTO>>.Fail(
                            $"Sản phẩm với ID {itemDto.ProductId} không tồn tại hoặc đã bị xóa.",
                            HttpStatusCode.BadRequest);
                    }

                    // Validate shop
                    if (product.ShopId != dto.ShopId)
                    {
                        return ServiceResponse<List<OrderResponseDTO>>.Fail(
                            $"Sản phẩm {product.Name} không thuộc shop này.",
                            HttpStatusCode.BadRequest);
                    }

                    // Validate stock
                    if (product.StockQuantity < itemDto.Quantity)
                    {
                        return ServiceResponse<List<OrderResponseDTO>>.Fail(
                            $"Sản phẩm {product.Name} không đủ số lượng tồn kho. Còn lại: {product.StockQuantity}",
                            HttpStatusCode.BadRequest);
                    }

                    // Tính tiền
                    decimal itemTotal = product.Price * itemDto.Quantity;
                    subtotal += itemTotal;

                    // Tạo OrderItem
                    orderItems.Add(new OrderItem
                    {
                        OrderId = Guid.Empty, // Sẽ được set sau khi tạo Order
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        PriceAtPurchase = product.Price
                    });
                }

                // 4. Parse PaymentMethod
                if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var paymentMethod))
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        $"Phương thức thanh toán không hợp lệ: {dto.PaymentMethod}",
                        HttpStatusCode.BadRequest);
                }

                // 5. Tính TotalAmount
                decimal totalAmount = subtotal + dto.ShippingFee - dto.DiscountAmount;
                if (totalAmount < 0)
                {
                    totalAmount = 0;
                }

                // 6. Tạo Order
                var newOrder = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = dto.CustomerId,
                    ShopId = dto.ShopId,
                    AddressId = dto.AddressId,
                    TotalAmount = totalAmount,
                    ShippingFee = dto.ShippingFee,
                    DiscountAmount = dto.DiscountAmount,
                    PromotionCodeUsed = dto.PromotionCode,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = PaymentStatus.Pending,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    Items = orderItems
                };

                // Set OrderId cho OrderItems
                foreach (var item in orderItems)
                {
                    item.OrderId = newOrder.Id;
                }

                // 7. Lưu Order (OrderItems sẽ được lưu tự động do cascade)
                await _orderRepo.CreateOrderAsync(newOrder);

                // 8. Load navigation properties để map đúng
                var savedOrder = await _orderRepo.GetByIdAsync(newOrder.Id);
                if (savedOrder == null)
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        "Lỗi khi tạo đơn hàng.",
                        HttpStatusCode.InternalServerError);
                }

                // Map order với navigation properties đã được load
                var orderDto = _mapper.Map<OrderResponseDTO>(savedOrder);

                return ServiceResponse<List<OrderResponseDTO>>.Success(
                    new List<OrderResponseDTO> { orderDto },
                    "Tạo đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<OrderResponseDTO>>.Fail(
                    $"Lỗi khi tạo đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> GetMyOrdersAsync(string customerId)
        {
            try
            {
                if (string.IsNullOrEmpty(customerId))
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        "CustomerId không được để trống.",
                        HttpStatusCode.BadRequest);
                }

                // Convert string to Guid nếu cần (nhưng CustomerId trong Order là string)
                // Nên ta giữ nguyên string và query trực tiếp
                var orders = await _orderRepo.GetOrdersByCustomerIdAsync(customerId);
                var orderDtos = _mapper.Map<List<OrderResponseDTO>>(orders);
                return ServiceResponse<List<OrderResponseDTO>>.Success(orderDtos, "Lấy danh sách đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<OrderResponseDTO>>.Fail(
                    $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> GetShopOrdersAsync(Guid shopId)
        {
            try
            {
                var orders = await _orderRepo.GetOrdersByShopIdAsync(shopId);
                var orderDtos = _mapper.Map<List<OrderResponseDTO>>(orders);
                return ServiceResponse<List<OrderResponseDTO>>.Success(orderDtos, "Lấy danh sách đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<OrderResponseDTO>>.Fail(
                    $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> GetAllOrdersAsync()
        {
            try
            {
                var orders = await _orderRepo.GetAllOrdersAsync();
                var orderDtos = _mapper.Map<List<OrderResponseDTO>>(orders);
                return ServiceResponse<List<OrderResponseDTO>>.Success(orderDtos, "Lấy danh sách đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<OrderResponseDTO>>.Fail(
                    $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> UpdateStatusAsync(Guid id, OrderUpdateStatusDTO dto)
        {
            try
            {
                // 1. Validate order exists
                var order = await _orderRepo.GetByIdAsync(id);
                if (order == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy đơn hàng.",
                        HttpStatusCode.NotFound);
                }

                // 2. Validate status transition
                if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
                {
                    return ServiceResponse<bool>.Fail(
                        $"Trạng thái không hợp lệ: {dto.Status}",
                        HttpStatusCode.BadRequest);
                }

                // 3. Validate status transition rules
                var currentStatus = order.Status;
                bool isValidTransition = newStatus switch
                {
                    OrderStatus.Confirmed => currentStatus == OrderStatus.Pending,
                    OrderStatus.Shipped => currentStatus == OrderStatus.Confirmed,
                    OrderStatus.Delivered => currentStatus == OrderStatus.Shipped,
                    OrderStatus.Canceled => currentStatus != OrderStatus.Delivered && currentStatus != OrderStatus.Canceled,
                    _ => false
                };

                if (!isValidTransition && newStatus != currentStatus)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể chuyển từ trạng thái {currentStatus} sang {newStatus}.",
                        HttpStatusCode.BadRequest);
                }

                // 4. Update status
                await _orderRepo.UpdateStatusAsync(id, dto.Status);

                return ServiceResponse<bool>.Success(true, "Cập nhật trạng thái đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi cập nhật trạng thái: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }
    }
}
