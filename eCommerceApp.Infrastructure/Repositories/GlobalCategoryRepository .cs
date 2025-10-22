// File: eCommerceApp.Infrastructure/Repositories/GlobalCategoryRepository.cs

using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class GlobalCategoryRepository : GenericRepository<GlobalCategory>, IGlobalCategoryRepository
    {
        private readonly AppDbContext _context;

        public GlobalCategoryRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<GlobalCategory>> GetAllCategoriesWithChildrenAsync()
        {
            // Lấy tất cả các danh mục cấp cao nhất (ParentId == null)
            // và bao gồm các danh mục con (Children) nếu bạn muốn trả về cấu trúc cây.
            // Nếu không cần cấu trúc cây, chỉ cần gọi GetAllAsync().

            return await _context.GlobalCategoris
                .Where(c => c.ParentId == null && !c.IsDeleted) // Chỉ lấy các Root Category
                .Include(c => c.Children) // Bao gồm 1 cấp con
                .ToListAsync();
        }
    }
}