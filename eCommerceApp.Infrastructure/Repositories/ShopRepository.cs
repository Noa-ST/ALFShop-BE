using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class ShopRepository : GenericRepository<Shop>, IShopRepository
    {
        private readonly AppDbContext _context;

        public ShopRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Shop>> GetBySellerIdAsync(string sellerId)
        {
            return await _context.Shops
                .Include(s => s.Seller)
                .Where(s => s.SellerId == sellerId && !s.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Shop>> GetAllActiveAsync()
        {
            return await _context.Shops
                .Include(s => s.Seller)
                .Where(s => !s.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
