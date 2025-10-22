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
    }
}