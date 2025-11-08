using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartByUserIdAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.Items)!
                    .ThenInclude(ci => ci.Product)!
                        .ThenInclude(p => p.Images!)
                .Include(c => c.Items)!
                    .ThenInclude(ci => ci.Product)!
                        .ThenInclude(p => p.Shop)
                .FirstOrDefaultAsync(c => c.CustomerId == userId && !c.IsDeleted);
        }

        public async Task<CartItem?> GetCartItemAsync(Guid cartId, Guid productId)
        {
            // Lấy một CartItem cụ thể để kiểm tra tồn tại hoặc cập nhật
            return await _context.CartItems
                .Where(ci => ci.CartId == cartId && ci.ProductId == productId)
                .FirstOrDefaultAsync();
        }

        // ✅ New: Explicit delete CartItem
        public async Task<int> RemoveCartItemAsync(Guid cartId, Guid productId)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);
            
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                return await _context.SaveChangesAsync();
            }
            
            return 0;
        }

        // ✅ New: Clear all items from cart
        public async Task<int> ClearCartItemsAsync(Guid cartId)
        {
            var cartItems = await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
            
            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                return await _context.SaveChangesAsync();
            }
            
            return 0;
        }
    }
}