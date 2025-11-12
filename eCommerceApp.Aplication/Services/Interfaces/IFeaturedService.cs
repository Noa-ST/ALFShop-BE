using eCommerceApp.Aplication.DTOs; // for ServiceResponse<>
using eCommerceApp.Aplication.DTOs.Featured;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IFeaturedService
    {
        Task<IEnumerable<FeaturedCategoryDto>> GetFeaturedCategoriesAsync(int limit, string? region);
        Task<IEnumerable<FeaturedShopDto>> GetFeaturedShopsAsync(int limit, string? city);
        Task<IEnumerable<FeaturedProductDto>> GetFeaturedProductsAsync(int limit, Guid? categoryId, decimal? priceMin, decimal? priceMax);

        Task<ServiceResponse<bool>> PinAsync(FeaturedPinRequest request);
        Task<ServiceResponse<IEnumerable<FeaturedScoreDebugDto>>> GetDebugScoresAsync(int topN = 20);
    }
}