using eCommerceApp.Domain.Entities;
namespace eCommerceApp.Domain.Interfaces
{
    public interface IShopCategoryRepository : IGeneric<ShopCategory>
    {
        Task<IEnumerable<ShopCategory>> GetByShopIdAsync(Guid shopId, bool includeChildren = false);

        Task<bool> IsShopCategoryOwnerAsync(Guid shopId, Guid categoryId);
    }
}
