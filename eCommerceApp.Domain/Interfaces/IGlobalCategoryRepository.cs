using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IGlobalCategoryRepository : IGeneric<GlobalCategory>
    {
        Task<IEnumerable<GlobalCategory>> GetAllCategoriesWithChildrenAsync();
        Task<int> CountChildrenAsync(Guid parentId);
        Task<bool> HasCircularReferenceAsync(Guid categoryId, Guid? newParentId);
        Task<GlobalCategory?> GetByIdWithChildrenAsync(Guid id);
        
        // ✅ New: Get all descendant IDs for a given category
        Task<List<Guid>> GetDescendantIdsAsync(Guid categoryId, bool includeSelf = false);
        
        // ✅ New: Check duplicate name in same level
        Task<bool> ExistsByNameInSameLevelAsync(string name, Guid? parentId, Guid? excludeId = null);
        
        // ✅ New: Statistics methods
        Task<int> GetTotalCountAsync();
        Task<int> GetRootCategoriesCountAsync();
        Task<int> GetMaxDepthAsync();
        Task<Dictionary<Guid, int>> GetProductCountPerCategoryAsync();
    }
}
