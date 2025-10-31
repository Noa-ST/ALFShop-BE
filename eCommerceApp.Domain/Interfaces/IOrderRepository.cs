using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId);
        Task<IEnumerable<Order>> GetOrdersByShopIdAsync(Guid shopId);
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order> GetByIdAsync(Guid id);
        Task UpdateStatusAsync(Guid orderId, string status);
    }
}
