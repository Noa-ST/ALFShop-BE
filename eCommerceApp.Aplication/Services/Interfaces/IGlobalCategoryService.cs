using eCommerceApp.Aplication.DTOs; 
using eCommerceApp.Aplication.DTOs.GlobalCategory;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IGlobalCategoryService
    {
        Task<ServiceResponse<GetGlobalCategory>> CreateGlobalCategoryAsync(CreateGlobalCategory dto);
        Task<ServiceResponse<bool>> UpdateGlobalCategoryAsync(Guid id, UpdateGlobalCategory dto);
        Task<ServiceResponse<bool>> DeleteGlobalCategoryAsync(Guid id); 
        Task<ServiceResponse<IEnumerable<GetGlobalCategory>>> GetAllGlobalCategoriesAsync(bool includeChildren = false);
        
        // ✅ New: Get category by ID
        Task<ServiceResponse<GetGlobalCategory>> GetGlobalCategoryByIdAsync(Guid id);
        
        // ✅ New: Get categories by parent ID
        Task<ServiceResponse<IEnumerable<GetGlobalCategory>>> GetCategoriesByParentIdAsync(Guid? parentId);

        // ✅ New: Get descendant IDs for a category
        Task<ServiceResponse<IEnumerable<Guid>>> GetDescendantIdsAsync(Guid categoryId, bool includeSelf = false);
        
        // ✅ New: Statistics for Admin dashboard
        Task<ServiceResponse<object>> GetStatisticsAsync();
    }
}