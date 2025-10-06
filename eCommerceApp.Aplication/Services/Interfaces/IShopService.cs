using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.DTOs;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IShopService
    {
        Task<ServiceResponse> CreateAsync(CreateShop shop);
        Task<ServiceResponse> UpdateAsync(UpdateShop shop);
        Task<ServiceResponse> DeleteAsync(Guid id);
        Task<GetShop?> GetByIdAsync(Guid id);
        Task<IEnumerable<GetShop>> GetAllActiveAsync();
        Task<IEnumerable<GetShop>> GetBySellerIdAsync(string sellerId);
    }
}
