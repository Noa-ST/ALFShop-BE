using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface ICategoryRepository : IGeneric<Category>
    {
        Task<IEnumerable<Category>> GetByShopIdAsync(Guid shopId);
        Task<Category?> GetByIdWithIncludeAsync(Guid id);

        Task<int> SoftDeleteAsync(Guid id);
    }
}
