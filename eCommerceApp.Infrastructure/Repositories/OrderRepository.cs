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

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId)
            => await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)!
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetOrdersByShopIdAsync(Guid shopId)
            => await _context.Orders
                .Where(o => o.ShopId == shopId)
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)!
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
            => await _context.Orders.Include(o => o.Customer).Include(o => o.Shop).ToListAsync();

        public async Task<Order> GetByIdAsync(Guid id)
            => await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .Include(o => o.Items)!
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id) ?? throw new Exception("Order not found");

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

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }
    }
}
