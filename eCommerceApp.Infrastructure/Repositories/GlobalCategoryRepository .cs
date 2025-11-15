// File: eCommerceApp.Infrastructure/Repositories/GlobalCategoryRepository.cs

using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

        // ✅ New: Get all descendant IDs (children, grandchildren, ...)
        public async Task<List<Guid>> GetDescendantIdsAsync(Guid categoryId, bool includeSelf = false)
        {
            // Load only Id and ParentId for efficiency
            var all = await _context.GlobalCategoris
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.ParentId })
                .ToListAsync();

            // Build lookup: parentId -> children Ids
            var childrenLookup = all
                .Where(c => c.ParentId.HasValue)
                .GroupBy(c => c.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var result = new List<Guid>();
            var stack = new Stack<Guid>();
            stack.Push(categoryId);

            // Optional: include self
            if (includeSelf)
                result.Add(categoryId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!childrenLookup.TryGetValue(current, out var children) || children.Count == 0)
                    continue;

                foreach (var childId in children)
                {
                    result.Add(childId);
                    stack.Push(childId);
                }
            }

            return result;
        }

        // ✅ New: Count children of a category
        public async Task<int> CountChildrenAsync(Guid parentId)
        {
            return await _context.GlobalCategoris
                .CountAsync(c => c.ParentId == parentId && !c.IsDeleted);
        }

        // ✅ New: Check for circular reference
        public async Task<bool> HasCircularReferenceAsync(Guid categoryId, Guid? newParentId)
        {
            if (!newParentId.HasValue)
                return false; // No parent = no circular reference

            // Kiểm tra xem newParentId có phải là descendant của categoryId không
            // Nếu có thì sẽ tạo circular reference (A -> B -> A)
            var currentId = newParentId.Value;
            var visited = new HashSet<Guid> { categoryId }; // Tránh infinite loop

            while (currentId != Guid.Empty)
            {
                if (visited.Contains(currentId))
                    return true; // Circular reference detected

                visited.Add(currentId);

                var parent = await _context.GlobalCategoris
                    .Where(c => c.Id == currentId && !c.IsDeleted)
                    .Select(c => c.ParentId)
                    .FirstOrDefaultAsync();

                if (!parent.HasValue)
                    break; // Reached root

                currentId = parent.Value;
            }

            return false;
        }

        // ✅ New: Get category by ID with children
        public async Task<GlobalCategory?> GetByIdWithChildrenAsync(Guid id)
        {
            var category = await _context.GlobalCategoris
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            
            if (category == null)
                return null;

            // Load children manually
            var children = await _context.GlobalCategoris
                .Where(c => c.ParentId == id && !c.IsDeleted)
                .ToListAsync();
            
            category.Children = children;
            return category;
        }

        // ✅ New: Check duplicate name in same level
        public async Task<bool> ExistsByNameInSameLevelAsync(string name, Guid? parentId, Guid? excludeId = null)
        {
            var query = _context.GlobalCategoris
                .Where(c => !c.IsDeleted 
                    && c.Name.ToLower() == name.ToLower() 
                    && c.ParentId == parentId);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        // ✅ New: Statistics methods
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.GlobalCategoris.CountAsync(c => !c.IsDeleted);
        }

        public async Task<int> GetRootCategoriesCountAsync()
        {
            return await _context.GlobalCategoris.CountAsync(c => !c.IsDeleted && c.ParentId == null);
        }

        public async Task<int> GetMaxDepthAsync()
        {
            var allCategories = await _context.GlobalCategoris
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.ParentId })
                .ToListAsync();

            if (!allCategories.Any())
                return 0;

            // Build parent lookup
            var parentLookup = allCategories
                .Where(c => c.ParentId.HasValue)
                .ToDictionary(c => c.Id, c => c.ParentId!.Value);

            // Calculate depth for each category
            int maxDepth = 1;

            foreach (var category in allCategories)
            {
                int depth = 1;
                Guid? currentId = category.Id;

                while (parentLookup.TryGetValue(currentId.Value, out var parentId))
                {
                    depth++;
                    currentId = parentId;
                    if (depth > 100) break; // Safety check for infinite loops
                }

                if (depth > maxDepth)
                    maxDepth = depth;
            }

            return maxDepth;
        }

        public async Task<Dictionary<Guid, int>> GetProductCountPerCategoryAsync()
        {
            return await _context.Products
                .Where(p => !p.IsDeleted)
                .GroupBy(p => p.GlobalCategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
        }
    }
}