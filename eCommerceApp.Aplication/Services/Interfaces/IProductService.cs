using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<GetProduct>> GetAllAsync();
        Task<GetProduct> GetByIdAsync(Guid id);
        Task<ServiceResponse> AddAsync(CreateProduct product);
        Task<ServiceResponse> UpdateAsync(UpdateProduct product);
        Task<ServiceResponse> DeleteAsync(Guid id); 
    }
}
