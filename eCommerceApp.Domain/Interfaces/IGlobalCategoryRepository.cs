using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IGlobalCategoryRepository : IGeneric<GlobalCategory>
    {
        Task<IEnumerable<GlobalCategory>> GetAllCategoriesWithChildrenAsync();
    }
}
