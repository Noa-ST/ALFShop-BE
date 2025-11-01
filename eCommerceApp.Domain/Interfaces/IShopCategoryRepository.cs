using eCommerceApp.Domain.Entities;
namespace eCommerceApp.Domain.Interfaces
{
    public interface IShopCategoryRepository : IGeneric<ShopCategory>
    {
        Task<IEnumerable<ShopCategory>> GetByShopIdAsync(Guid shopId, bool includeChildren = false);
        Task<bool> IsShopCategoryOwnerAsync(Guid shopId, Guid categoryId);
        
        // ✅ New: Check duplicate name in same shop and level
        Task<bool> ExistsByNameInSameShopAndLevelAsync(string name, Guid shopId, Guid? parentId, Guid? excludeId = null);
        
        // ✅ New: Check for circular reference
        Task<bool> HasCircularReferenceAsync(Guid categoryId, Guid? newParentId);
        
        // ✅ New: Count children of a category
        Task<int> CountChildrenAsync(Guid categoryId);
        
        // ✅ New: Get category by ID with children (for update)
        Task<ShopCategory?> GetByIdWithChildrenAsync(Guid id);
    }
}
