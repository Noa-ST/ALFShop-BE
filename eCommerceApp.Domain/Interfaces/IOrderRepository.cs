using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId);
        Task<IEnumerable<Order>> GetOrdersByShopIdAsync(Guid shopId);
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order?> GetByIdAsync(Guid id); // ✅ Fix: Return nullable
        Task UpdateStatusAsync(Guid orderId, string status);
        Task UpdateOrderAsync(Order order);
        
        // ✅ New: Pagination & Filtering
        Task<(IEnumerable<Order> Orders, int TotalCount)> SearchAndFilterAsync(
            string? keyword,
            OrderStatus? status,
            Guid? shopId,
            string? customerId,
            DateTime? startDate,
            DateTime? endDate,
            decimal? minAmount,
            decimal? maxAmount,
            string sortBy,
            string sortOrder,
            int page,
            int pageSize);
        
        // ✅ New: Statistics
        Task<int> GetTotalCountAsync();
        Task<int> GetCountByStatusAsync(OrderStatus status);
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetAverageOrderValueAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<OrderStatus, int>> GetOrdersByStatusAsync();
        Task<List<(Guid ShopId, string ShopName, int OrderCount, decimal Revenue)>> GetTopShopsByOrdersAsync(int top = 10);
        Task<List<(string CustomerId, string CustomerName, int OrderCount, decimal TotalSpent)>> GetTopCustomersAsync(int top = 10);
        
        // ✅ New: Update Tracking Number
        Task UpdateTrackingNumberAsync(Guid orderId, string trackingNumber);
        
        // ✅ New: Create order with transaction support (for stock reduction)
        Task<Order> CreateOrderWithTransactionAsync(Order order, Func<Task>? beforeSave = null);
    }
}
