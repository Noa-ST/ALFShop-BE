using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<GetProduct>> GetAllAsync();
        Task<IEnumerable<GetProduct>> GetByShopIdAsync(Guid shopId);
        Task<IEnumerable<GetProduct>> GetByGlobalCategoryIdAsync(Guid globalCategoryId);
        Task<GetProductDetail?> GetDetailByIdAsync(Guid id);
        Task<ServiceResponse> AddAsync(CreateProduct product);
        Task<ServiceResponse> UpdateAsync(Guid id, UpdateProduct product);
        Task<ServiceResponse> DeleteAsync(Guid id);
    }
}
