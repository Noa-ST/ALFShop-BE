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
            // Lấy Cart của Customer, bao gồm CartItems và thông tin Sản phẩm chi tiết
            return await _context.Carts
                // Bao gồm CartItems
                .Include(c => c.Items)!
                    // Bao gồm thông tin Product
                    .ThenInclude(ci => ci.Product)!
                        // Bao gồm Images, cần cho hiển thị ItemTotal và Product Price
                        .ThenInclude(p => p.Images)
                .Include(c => c.Items)!
                    .ThenInclude(ci => ci.Product)!
                        // Bao gồm Shop (để tính Subtotal theo Shop)
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
    }
}