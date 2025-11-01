using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Order;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Domain.Interfaces;
using System.Net;
using Microsoft.EntityFrameworkCore;
using eCommerceApp.Infrastructure.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly IAddressRepository _addressRepo;
        private readonly IShopRepository _shopRepo;
        private readonly IMapper _mapper;
        private readonly IProductService _productService;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor; // ✅ New: Optimize admin check

        public OrderService(
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            IAddressRepository addressRepo,
            IShopRepository shopRepo,
            IMapper mapper,
            IProductService productService,
            AppDbContext dbContext,
            IHttpContextAccessor httpContextAccessor) // ✅ New: Inject IHttpContextAccessor
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _addressRepo = addressRepo;
            _shopRepo = shopRepo;
            _mapper = mapper;
            _productService = productService;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        // ✅ New: Helper method để check admin role hiệu quả hơn
        private bool IsAdmin(string? userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
            }
            // Nếu có userId, vẫn check từ HttpContext vì đó là user hiện tại
            return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
        }

        public async Task<ServiceResponse<List<OrderResponseDTO>>> CreateOrderAsync(OrderCreateDTO dto)
        {
            // ✅ Fix: Use transaction để đảm bảo consistency
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate CustomerId
                if (string.IsNullOrEmpty(dto.CustomerId))
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        "CustomerId không được để trống.",
                        HttpStatusCode.BadRequest);
                }

                // ✅ New: Validate ShopId exists
                var shop = await _shopRepo.GetByIdAsync(dto.ShopId);
                if (shop == null || shop.IsDeleted)
                {
                    return ServiceResponse<List<OrderResponseDTO>>.Fail(
                        "Shop không tồn tại hoặc đã bị xóa.",
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

                // 7. ✅ Fix: Giảm stock TRƯỚC khi tạo order (trong transaction)
                foreach (var item in orderItems)
                {
                    var stockResult = await _productService.ReduceStockAsync(item.ProductId, item.Quantity);
                    if (!stockResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return ServiceResponse<List<OrderResponseDTO>>.Fail(
                            $"Không thể giảm số lượng tồn kho cho sản phẩm {item.ProductId}. {stockResult.Message}",
                            HttpStatusCode.BadRequest);
                    }
                }

                // 8. Lưu Order (OrderItems sẽ được lưu tự động do cascade)
                await _orderRepo.CreateOrderAsync(newOrder);

                // 9. Commit transaction
                await transaction.CommitAsync();

                // 10. Load navigation properties để map đúng
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
                await _dbContext.Database.CurrentTransaction?.RollbackAsync();
                return ServiceResponse<List<OrderResponseDTO>>.Fail(
                    $"Lỗi khi tạo đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<PagedResult<OrderResponseDTO>>> GetMyOrdersAsync(string customerId, OrderFilterDto? filter = null)
        {
            try
            {
                if (string.IsNullOrEmpty(customerId))
                {
                    return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                        "CustomerId không được để trống.",
                        HttpStatusCode.BadRequest);
                }

                // Use filter nếu có, nếu không tạo default filter
                filter ??= new OrderFilterDto { Page = 1, PageSize = 20 };
                filter.Validate();
                filter.CustomerId = customerId; // Force customerId filter

                var (orders, totalCount) = await _orderRepo.SearchAndFilterAsync(
                    filter.Keyword,
                    filter.Status,
                    filter.ShopId,
                    filter.CustomerId,
                    filter.StartDate,
                    filter.EndDate,
                    filter.MinAmount,
                    filter.MaxAmount,
                    filter.SortBy,
                    filter.SortOrder,
                    filter.Page,
                    filter.PageSize);

                var orderDtos = _mapper.Map<List<OrderResponseDTO>>(orders);
                var result = new PagedResult<OrderResponseDTO>
                {
                    Data = orderDtos,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount
                };

                return ServiceResponse<PagedResult<OrderResponseDTO>>.Success(result, "Lấy danh sách đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                    $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<PagedResult<OrderResponseDTO>>> GetShopOrdersAsync(Guid shopId, string? userId = null, OrderFilterDto? filter = null)
        {
            try
            {
                // ✅ Fix: Validate shop ownership (Seller chỉ xem được order của shop mình, Admin xem được tất cả)
                if (!string.IsNullOrEmpty(userId))
                {
                    var shop = await _shopRepo.GetByIdAsync(shopId);
                    if (shop == null || shop.IsDeleted)
                    {
                        return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                            "Shop không tồn tại hoặc đã bị xóa.",
                            HttpStatusCode.NotFound);
                    }

                    // ✅ Optimize: Check admin role using IHttpContextAccessor
                    var isAdmin = IsAdmin(userId);

                    if (!isAdmin && shop.SellerId != userId)
                    {
                        return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                            "Bạn không có quyền xem đơn hàng của shop này.",
                            HttpStatusCode.Forbidden);
                    }
                }

                // Use filter nếu có, nếu không tạo default filter
                filter ??= new OrderFilterDto { Page = 1, PageSize = 20 };
                filter.Validate();
                filter.ShopId = shopId; // Force shopId filter

                var (orders, totalCount) = await _orderRepo.SearchAndFilterAsync(
                    filter.Keyword,
                    filter.Status,
                    filter.ShopId,
                    filter.CustomerId,
                    filter.StartDate,
                    filter.EndDate,
                    filter.MinAmount,
                    filter.MaxAmount,
                    filter.SortBy,
                    filter.SortOrder,
                    filter.Page,
                    filter.PageSize);

                var orderDtos = _mapper.Map<List<OrderResponseDTO>>(orders);
                var result = new PagedResult<OrderResponseDTO>
                {
                    Data = orderDtos,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount
                };

                return ServiceResponse<PagedResult<OrderResponseDTO>>.Success(result, "Lấy danh sách đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                    $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<PagedResult<OrderResponseDTO>>> GetAllOrdersAsync(OrderFilterDto? filter = null)
        {
            try
            {
                // Use filter nếu có, nếu không tạo default filter
                filter ??= new OrderFilterDto { Page = 1, PageSize = 20 };
                filter.Validate();

                var (orders, totalCount) = await _orderRepo.SearchAndFilterAsync(
                    filter.Keyword,
                    filter.Status,
                    filter.ShopId,
                    filter.CustomerId,
                    filter.StartDate,
                    filter.EndDate,
                    filter.MinAmount,
                    filter.MaxAmount,
                    filter.SortBy,
                    filter.SortOrder,
                    filter.Page,
                    filter.PageSize);

                var orderDtos = _mapper.Map<List<OrderResponseDTO>>(orders);
                var result = new PagedResult<OrderResponseDTO>
                {
                    Data = orderDtos,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount
                };

                return ServiceResponse<PagedResult<OrderResponseDTO>>.Success(result, "Lấy danh sách đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                    $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> UpdateStatusAsync(Guid id, OrderUpdateStatusDTO dto, string? userId = null)
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

                // ✅ Fix: Validate ownership
                if (!string.IsNullOrEmpty(userId))
                {
                    // ✅ Optimize: Check admin role using IHttpContextAccessor
                    var isAdmin = IsAdmin(userId);

                    var isCustomer = order.CustomerId == userId;
                    var shop = await _shopRepo.GetByIdAsync(order.ShopId);
                    var isShopOwner = shop != null && shop.SellerId == userId;

                    // Validate permissions based on status transition
                    if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
                    {
                        return ServiceResponse<bool>.Fail(
                            $"Trạng thái không hợp lệ: {dto.Status}",
                            HttpStatusCode.BadRequest);
                    }

                    // Customer chỉ có thể cancel order của mình
                    if (newStatus == OrderStatus.Canceled)
                    {
                        if (!isAdmin && !isCustomer)
                        {
                            return ServiceResponse<bool>.Fail(
                                "Chỉ khách hàng hoặc Admin mới có thể hủy đơn hàng.",
                                HttpStatusCode.Forbidden);
                        }
                    }
                    // Seller chỉ có thể update order của shop mình (Confirmed, Shipped, Delivered)
                    else if (newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Delivered)
                    {
                        if (!isAdmin && !isShopOwner)
                        {
                            return ServiceResponse<bool>.Fail(
                                "Bạn không có quyền cập nhật đơn hàng này. Chỉ có thể cập nhật đơn hàng của shop bạn.",
                                HttpStatusCode.Forbidden);
                        }
                    }
                }

                // 2. Validate status transition
                if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var parsedNewStatus))
                {
                    return ServiceResponse<bool>.Fail(
                        $"Trạng thái không hợp lệ: {dto.Status}",
                        HttpStatusCode.BadRequest);
                }

                // 3. Validate status transition rules
                var currentStatus = order.Status;
                bool isValidTransition = parsedNewStatus switch
                {
                    OrderStatus.Confirmed => currentStatus == OrderStatus.Pending,
                    OrderStatus.Shipped => currentStatus == OrderStatus.Confirmed,
                    OrderStatus.Delivered => currentStatus == OrderStatus.Shipped,
                    OrderStatus.Canceled => currentStatus != OrderStatus.Delivered && currentStatus != OrderStatus.Canceled,
                    _ => false
                };

                if (!isValidTransition && parsedNewStatus != currentStatus)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể chuyển từ trạng thái {currentStatus} sang {parsedNewStatus}.",
                        HttpStatusCode.BadRequest);
                }

                // 4. Update status
                await _orderRepo.UpdateStatusAsync(id, dto.Status);

                // ✅ Fix: Restore stock khi order bị cancel (trong transaction nếu có thể)
                if (parsedNewStatus == OrderStatus.Canceled && currentStatus != OrderStatus.Canceled)
                {
                    try
                    {
                        // Load order với items để restore stock
                        var orderWithItems = await _orderRepo.GetByIdAsync(id);
                        if (orderWithItems?.Items != null)
                        {
                            foreach (var item in orderWithItems.Items)
                            {
                                var stockResult = await _productService.RestoreStockAsync(item.ProductId, item.Quantity);
                                if (!stockResult.Succeeded)
                                {
                                    // Log warning nhưng không fail status update
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error nhưng không fail status update
                        // Trong production nên có logging service
                    }
                }

                return ServiceResponse<bool>.Success(true, "Cập nhật trạng thái đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi cập nhật trạng thái: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: GET /api/Order/{id}
        public async Task<ServiceResponse<OrderResponseDTO>> GetOrderByIdAsync(Guid id, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<OrderResponseDTO>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                var order = await _orderRepo.GetByIdAsync(id);
                if (order == null)
                {
                    return ServiceResponse<OrderResponseDTO>.Fail(
                        "Không tìm thấy đơn hàng.",
                        HttpStatusCode.NotFound);
                }

                // ✅ Optimize: Validate ownership using optimized admin check
                var isAdmin = IsAdmin(userId);

                var isCustomer = order.CustomerId == userId;
                var shop = await _shopRepo.GetByIdAsync(order.ShopId);
                var isShopOwner = shop != null && shop.SellerId == userId;

                if (!isAdmin && !isCustomer && !isShopOwner)
                {
                    return ServiceResponse<OrderResponseDTO>.Fail(
                        "Bạn không có quyền xem đơn hàng này.",
                        HttpStatusCode.Forbidden);
                }

                var orderDto = _mapper.Map<OrderResponseDTO>(order);
                return ServiceResponse<OrderResponseDTO>.Success(orderDto, "Lấy thông tin đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<OrderResponseDTO>.Fail(
                    $"Lỗi khi lấy thông tin đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Search and Filter with Pagination
        public async Task<ServiceResponse<PagedResult<OrderResponseDTO>>> SearchAndFilterAsync(OrderFilterDto filter, string? userId = null)
        {
            try
            {
                filter.Validate();

                // Nếu có userId, validate ownership (Customer chỉ xem được order của mình, Seller xem được order của shop mình, Admin xem được tất cả)
                if (!string.IsNullOrEmpty(userId))
                {
                    var isAdmin = IsAdmin(userId);
                    
                    // Nếu không phải admin, chỉ cho phép filter theo customerId hoặc shopId của user
                    if (!isAdmin)
                    {
                        // Check nếu user là customer
                        if (!string.IsNullOrEmpty(filter.CustomerId) && filter.CustomerId != userId)
                        {
                            return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                                "Bạn không có quyền xem đơn hàng của khách hàng khác.",
                                HttpStatusCode.Forbidden);
                        }

                        // Check nếu user là seller
                        if (filter.ShopId.HasValue)
                        {
                            var shop = await _shopRepo.GetByIdAsync(filter.ShopId.Value);
                            if (shop == null || shop.SellerId != userId)
                            {
                                return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                                    "Bạn không có quyền xem đơn hàng của shop này.",
                                    HttpStatusCode.Forbidden);
                            }
                        }
                        else
                        {
                            // Nếu không có shopId filter, chỉ cho phép xem order của chính user (customer)
                            filter.CustomerId = userId;
                        }
                    }
                }

                var (orders, totalCount) = await _orderRepo.SearchAndFilterAsync(
                    filter.Keyword,
                    filter.Status,
                    filter.ShopId,
                    filter.CustomerId,
                    filter.StartDate,
                    filter.EndDate,
                    filter.MinAmount,
                    filter.MaxAmount,
                    filter.SortBy,
                    filter.SortOrder,
                    filter.Page,
                    filter.PageSize);

                var orderDtos = _mapper.Map<List<OrderResponseDTO>>(orders);
                var result = new PagedResult<OrderResponseDTO>
                {
                    Data = orderDtos,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount
                };

                return ServiceResponse<PagedResult<OrderResponseDTO>>.Success(result, "Tìm kiếm đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<PagedResult<OrderResponseDTO>>.Fail(
                    $"Lỗi khi tìm kiếm đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Statistics for Admin dashboard
        public async Task<ServiceResponse<object>> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var totalOrders = await _orderRepo.GetTotalCountAsync();
                var ordersByStatus = await _orderRepo.GetOrdersByStatusAsync();
                var totalRevenue = await _orderRepo.GetTotalRevenueAsync(startDate, endDate);
                var averageOrderValue = await _orderRepo.GetAverageOrderValueAsync(startDate, endDate);
                var topShops = await _orderRepo.GetTopShopsByOrdersAsync(10);
                var topCustomers = await _orderRepo.GetTopCustomersAsync(10);

                var statistics = new
                {
                    TotalOrders = totalOrders,
                    OrdersByStatus = ordersByStatus,
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = averageOrderValue,
                    TopShops = topShops.Select(s => new
                    {
                        ShopId = s.ShopId,
                        ShopName = s.ShopName,
                        OrderCount = s.OrderCount,
                        Revenue = s.Revenue
                    }).ToList(),
                    TopCustomers = topCustomers.Select(c => new
                    {
                        CustomerId = c.CustomerId,
                        CustomerName = c.CustomerName,
                        OrderCount = c.OrderCount,
                        TotalSpent = c.TotalSpent
                    }).ToList(),
                    DateRange = new
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    }
                };

                return ServiceResponse<object>.Success(statistics, "Lấy thống kê đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<object>.Fail(
                    $"Lỗi khi lấy thống kê: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Cancel Order Endpoint
        public async Task<ServiceResponse<bool>> CancelOrderAsync(Guid id, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                var order = await _orderRepo.GetByIdAsync(id);
                if (order == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy đơn hàng.",
                        HttpStatusCode.NotFound);
                }

                // Validate ownership: Customer chỉ có thể cancel order của mình, Admin có thể cancel tất cả
                var isAdmin = IsAdmin(userId);
                var isCustomer = order.CustomerId == userId;

                if (!isAdmin && !isCustomer)
                {
                    return ServiceResponse<bool>.Fail(
                        "Bạn không có quyền hủy đơn hàng này.",
                        HttpStatusCode.Forbidden);
                }

                // Validate status: Chỉ có thể cancel khi status là Pending hoặc Confirmed
                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể hủy đơn hàng ở trạng thái {order.Status}. Chỉ có thể hủy đơn hàng ở trạng thái Pending hoặc Confirmed.",
                        HttpStatusCode.BadRequest);
                }

                // Update status to Canceled
                await _orderRepo.UpdateStatusAsync(id, OrderStatus.Canceled.ToString());

                // Restore stock
                if (order.Items != null)
                {
                    foreach (var item in order.Items)
                    {
                        var stockResult = await _productService.RestoreStockAsync(item.ProductId, item.Quantity);
                        if (!stockResult.Succeeded)
                        {
                            // Log warning nhưng không fail cancel operation
                        }
                    }
                }

                return ServiceResponse<bool>.Success(true, "Hủy đơn hàng thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi hủy đơn hàng: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        // ✅ New: Update Tracking Number
        public async Task<ServiceResponse<bool>> UpdateTrackingNumberAsync(Guid id, string trackingNumber, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể xác định người dùng.",
                        HttpStatusCode.Unauthorized);
                }

                if (string.IsNullOrWhiteSpace(trackingNumber))
                {
                    return ServiceResponse<bool>.Fail(
                        "Mã vận chuyển không được để trống.",
                        HttpStatusCode.BadRequest);
                }

                var order = await _orderRepo.GetByIdAsync(id);
                if (order == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy đơn hàng.",
                        HttpStatusCode.NotFound);
                }

                // Validate ownership: Seller chỉ có thể update tracking number của shop mình, Admin có thể update tất cả
                var isAdmin = IsAdmin(userId);
                var shop = await _shopRepo.GetByIdAsync(order.ShopId);
                var isShopOwner = shop != null && shop.SellerId == userId;

                if (!isAdmin && !isShopOwner)
                {
                    return ServiceResponse<bool>.Fail(
                        "Bạn không có quyền cập nhật mã vận chuyển cho đơn hàng này.",
                        HttpStatusCode.Forbidden);
                }

                // Validate status: Chỉ có thể update tracking number khi order đã được shipped hoặc delivered
                if (order.Status != OrderStatus.Shipped && order.Status != OrderStatus.Delivered)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Chỉ có thể cập nhật mã vận chuyển khi đơn hàng ở trạng thái Shipped hoặc Delivered. Trạng thái hiện tại: {order.Status}",
                        HttpStatusCode.BadRequest);
                }

                await _orderRepo.UpdateTrackingNumberAsync(id, trackingNumber.Trim());
                return ServiceResponse<bool>.Success(true, "Cập nhật mã vận chuyển thành công.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi cập nhật mã vận chuyển: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }
    }
}
