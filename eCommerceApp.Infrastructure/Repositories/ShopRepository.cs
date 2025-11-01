using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

        // ✅ New: Search and filter with pagination
        public async Task<(IEnumerable<Shop> Shops, int TotalCount)> SearchAndFilterAsync(
            string? keyword,
            string? city,
            string? country,
            float? minRating,
            float? maxRating,
            string sortBy,
            string sortOrder,
            int page,
            int pageSize)
        {
            var query = _context.Shops
                .Include(s => s.Seller)
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(s => 
                    s.Name.ToLower().Contains(keyword) || 
                    (s.Description != null && s.Description.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s => s.City.ToLower() == city.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(s => s.Country != null && s.Country.ToLower() == country.Trim().ToLower());
            }

            if (minRating.HasValue)
                query = query.Where(s => s.AverageRating >= minRating.Value);

            if (maxRating.HasValue)
                query = query.Where(s => s.AverageRating <= maxRating.Value);

            // Get total count before pagination
            int totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(s => s.Name) 
                    : query.OrderByDescending(s => s.Name),
                "rating" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(s => s.AverageRating) 
                    : query.OrderByDescending(s => s.AverageRating),
                "city" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(s => s.City) 
                    : query.OrderByDescending(s => s.City),
                "updatedat" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(s => s.UpdatedAt ?? s.CreatedAt) 
                    : query.OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt),
                _ => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(s => s.CreatedAt) 
                    : query.OrderByDescending(s => s.CreatedAt) // Default: createdAt
            };

            // Apply pagination
            var shops = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (shops, totalCount);
        }

        // ✅ New: Get shop by ID for update (with tracking)
        public async Task<Shop?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Shops
                .Include(s => s.Seller)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        }

        // ✅ New: Statistics methods
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Shops.CountAsync(s => !s.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetShopsByCityAsync()
        {
            return await _context.Shops
                .Where(s => !s.IsDeleted)
                .GroupBy(s => s.City)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.City, x => x.Count);
        }

        public async Task<Dictionary<Guid, int>> GetProductCountPerShopAsync()
        {
            return await _context.Products
                .Where(p => !p.IsDeleted)
                .GroupBy(p => p.ShopId)
                .Select(g => new { ShopId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ShopId, x => x.Count);
        }
    }
}
