// File: eCommerceApp.Infrastructure/Repositories/ShopCategoryRepository.cs

using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class ShopCategoryRepository : GenericRepository<ShopCategory>, IShopCategoryRepository
    {
        private readonly AppDbContext _context;

        public ShopCategoryRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ShopCategory>> GetByShopIdAsync(Guid shopId, bool includeChildren = false)
        {
            var query = _context.ShopCategories
                .Where(sc => sc.ShopId == shopId && !sc.IsDeleted);

            if (includeChildren)
            {
                // Chỉ lấy các danh mục cấp cao nhất cho Shop và bao gồm children
                query = query.Where(sc => sc.ParentId == null);
                // 💡 Có thể cần logic Include đệ quy nếu cần nhiều hơn 1 cấp
                query = query.Include(sc => sc.Children);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> IsShopCategoryOwnerAsync(Guid shopId, Guid categoryId)
        {
            return await _context.ShopCategories
                .AnyAsync(sc => sc.Id == categoryId && sc.ShopId == shopId && !sc.IsDeleted);
        }

        // Cần override GetByIdAsync hoặc GetSingleAsync để thêm ShopId check cho bảo mật
        public override async Task<ShopCategory?> GetByIdAsync(Guid id)
        {
            // Dù không lý tưởng, nhưng cần đảm bảo chỉ lấy các Category chưa bị xóa
            return await _context.ShopCategories
                .FirstOrDefaultAsync(sc => sc.Id == id && !sc.IsDeleted);
        }
    }
}