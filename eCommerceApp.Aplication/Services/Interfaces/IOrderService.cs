using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Order;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ServiceResponse<List<OrderResponseDTO>>> CreateOrderAsync(OrderCreateDTO dto);
        Task<ServiceResponse<PagedResult<OrderResponseDTO>>> GetMyOrdersAsync(string customerId, OrderFilterDto? filter = null);
        Task<ServiceResponse<PagedResult<OrderResponseDTO>>> GetShopOrdersAsync(Guid shopId, string? userId = null, OrderFilterDto? filter = null);
        Task<ServiceResponse<PagedResult<OrderResponseDTO>>> GetAllOrdersAsync(OrderFilterDto? filter = null);
        Task<ServiceResponse<bool>> UpdateStatusAsync(Guid id, OrderUpdateStatusDTO dto, string? userId = null);
        Task<ServiceResponse<OrderResponseDTO>> GetOrderByIdAsync(Guid id, string userId);
        
        // ✅ New: Pagination & Filtering
        Task<ServiceResponse<PagedResult<OrderResponseDTO>>> SearchAndFilterAsync(OrderFilterDto filter, string? userId = null);
        
        // ✅ New: Statistics
        Task<ServiceResponse<object>> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        // ✅ New: Cancel Order
        Task<ServiceResponse<bool>> CancelOrderAsync(Guid id, string userId);
        
        // ✅ New: Update Tracking Number
        Task<ServiceResponse<bool>> UpdateTrackingNumberAsync(Guid id, string trackingNumber, string userId);
        
        // ✅ New: Customer Confirm Delivery
        Task<ServiceResponse<bool>> ConfirmDeliveryAsync(Guid id, string userId);
        
        // New: Shop revenue summary
        Task<ServiceResponse<eCommerceApp.Aplication.DTOs.Order.ShopRevenueSummaryDto>> GetShopRevenueSummaryAsync(
            Guid shopId,
            DateTime? from = null,
            DateTime? to = null,
            string? groupBy = "day",
            bool onlyPaid = true,
            eCommerceApp.Domain.Enums.OrderStatus? status = eCommerceApp.Domain.Enums.OrderStatus.Delivered,
            IEnumerable<eCommerceApp.Domain.Enums.PaymentMethod>? paymentMethods = null);
    }
}

// ✅ NOTE: Khi có ReviewService, cần tích hợp:
// - Khi review được approve: await _productService.RecalculateRatingAsync(review.ProductId);
// - Khi review bị xóa: await _productService.RecalculateRatingAsync(review.ProductId);
// - Khi review được update: await _productService.RecalculateRatingAsync(review.ProductId);