using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả category thuộc một shop, bao gồm thông tin Shop và Product.
        /// </summary>
        public async Task<IEnumerable<Category>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Categories
                .Include(c => c.Shop)
                .Include(c => c.Products)
                .Where(c => c.ShopId == shopId && !c.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Lấy category theo ID (có Include Shop và Products).
        /// </summary>
        public async Task<Category?> GetByIdWithIncludeAsync(Guid id)
        {
            return await _context.Categories
                .Include(c => c.Shop)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        /// <summary>
        /// Thực hiện soft delete + cập nhật UpdatedAt.
        /// </summary>
        public async Task<int> SoftDeleteAsync(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return 0;

            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Update(category);
            return await _context.SaveChangesAsync();
        }
    }
}
