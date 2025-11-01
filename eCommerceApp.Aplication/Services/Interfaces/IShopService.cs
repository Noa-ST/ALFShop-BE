using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.DTOs;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IShopService
    {
        Task<ServiceResponse> CreateAsync(CreateShop shop);
        Task<ServiceResponse> UpdateAsync(UpdateShop shop, string? userId = null);
        Task<ServiceResponse> DeleteAsync(Guid id, string? userId = null);
        Task<GetShop?> GetByIdAsync(Guid id);
        Task<IEnumerable<GetShop>> GetAllActiveAsync();
        Task<IEnumerable<GetShop>> GetBySellerIdAsync(string sellerId);
        
        // ✅ New: Search and filter with pagination
        Task<PagedResult<GetShop>> SearchAndFilterAsync(ShopFilterDto filter);
        
        // ✅ New: Rating management
        Task<ServiceResponse> RecalculateRatingAsync(Guid shopId);
        
        // ✅ New: Statistics for Admin dashboard
        Task<ServiceResponse<object>> GetStatisticsAsync();
    }
}
