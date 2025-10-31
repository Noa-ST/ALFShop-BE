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
            // Nạp toàn bộ danh mục (không xoá)
            var allCategories = await _context.GlobalCategoris
                .Where(c => !c.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            // Lấy danh mục gốc (ParentId == null)
            var roots = allCategories.Where(c => c.ParentId == null).ToList();

            // Tạo lookup các con theo ParentId (loại bỏ null)
            var childrenLookup = allCategories
                .Where(c => c.ParentId.HasValue)
                .ToLookup(c => c.ParentId!.Value);

            void BuildChildren(GlobalCategory node)
            {
                var children = childrenLookup[node.Id].ToList();
                node.Children = children;

                foreach (var child in children)
                {
                    BuildChildren(child);
                }
            }

            foreach (var root in roots)
            {
                BuildChildren(root);
            }

            return roots;
        }
    }
}