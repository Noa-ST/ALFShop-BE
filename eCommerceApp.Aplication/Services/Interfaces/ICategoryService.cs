using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Category;
using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ServiceResponse> AddAsync(CreateCategory category);
        Task<ServiceResponse> UpdateAsync(UpdateCategory category);
        Task<ServiceResponse> DeleteAsync(Guid id);
        Task<IEnumerable<GetCategory>> GetAllAsync();
        Task<GetCategory?> GetByIdAsync(Guid id);
        Task<IEnumerable<GetCategory>> GetByShopIdAsync(Guid shopId);
    }
}
