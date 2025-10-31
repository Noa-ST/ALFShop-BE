using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId)
    => await _context.Orders
        .Where(o => o.CustomerId == customerId.ToString())
        .ToListAsync();

        public async Task<IEnumerable<Order>> GetOrdersByShopIdAsync(Guid shopId)
            => await _context.Orders
                .Where(o => o.ShopId == shopId)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
            => await _context.Orders.Include(o => o.Customer).Include(o => o.Shop).ToListAsync();

        public async Task<Order> GetByIdAsync(Guid id)
            => await _context.Orders.FindAsync(id);

        public async Task UpdateStatusAsync(Guid orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new Exception("Order not found");

            if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
                throw new Exception($"Invalid order status: {status}");

            order.Status = parsedStatus;
            await _context.SaveChangesAsync();
        }

    }
}
