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

        // ✅ New: Check duplicate name in same shop and level
        public async Task<bool> ExistsByNameInSameShopAndLevelAsync(string name, Guid shopId, Guid? parentId, Guid? excludeId = null)
        {
            var query = _context.ShopCategories
                .Where(sc => !sc.IsDeleted 
                    && sc.ShopId == shopId
                    && sc.Name.ToLower() == name.ToLower() 
                    && sc.ParentId == parentId);

            if (excludeId.HasValue)
            {
                query = query.Where(sc => sc.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        // ✅ New: Check for circular reference
        public async Task<bool> HasCircularReferenceAsync(Guid categoryId, Guid? newParentId)
        {
            if (!newParentId.HasValue)
                return false; // No parent = no circular reference

            // Kiểm tra xem newParentId có phải là descendant của categoryId không
            var currentId = newParentId.Value;
            var visited = new HashSet<Guid> { categoryId }; // Tránh infinite loop

            while (currentId != Guid.Empty)
            {
                if (visited.Contains(currentId))
                    return true; // Circular reference detected

                visited.Add(currentId);

                var parent = await _context.ShopCategories
                    .Where(sc => sc.Id == currentId && !sc.IsDeleted)
                    .Select(sc => sc.ParentId)
                    .FirstOrDefaultAsync();

                if (!parent.HasValue)
                    break; // Reached root

                currentId = parent.Value;
            }

            return false;
        }

        // ✅ New: Count children of a category
        public async Task<int> CountChildrenAsync(Guid categoryId)
        {
            return await _context.ShopCategories
                .CountAsync(sc => sc.ParentId == categoryId && !sc.IsDeleted);
        }

        // ✅ New: Get category by ID with children (for update)
        public async Task<ShopCategory?> GetByIdWithChildrenAsync(Guid id)
        {
            var category = await _context.ShopCategories
                .FirstOrDefaultAsync(sc => sc.Id == id && !sc.IsDeleted);
            
            if (category == null)
                return null;

            // Load children manually
            var children = await _context.ShopCategories
                .Where(sc => sc.ParentId == id && !sc.IsDeleted)
                .ToListAsync();
            
            category.Children = children;
            return category;
        }
    }
}