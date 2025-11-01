using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IShopRepository : IGeneric<Shop>
    {
        Task<IEnumerable<Shop>> GetBySellerIdAsync(string sellerId);
        Task<IEnumerable<Shop>> GetAllActiveAsync(); // IsDeleted == false
        
        // ✅ New: Search and filter with pagination
        Task<(IEnumerable<Shop> Shops, int TotalCount)> SearchAndFilterAsync(
            string? keyword,
            string? city,
            string? country,
            float? minRating,
            float? maxRating,
            string sortBy,
            string sortOrder,
            int page,
            int pageSize);
        
        // ✅ New: Get shop by ID for update (with tracking)
        Task<Shop?> GetByIdForUpdateAsync(Guid id);
        
        // ✅ New: Statistics methods
        Task<int> GetTotalCountAsync();
        Task<Dictionary<string, int>> GetShopsByCityAsync();
        Task<Dictionary<Guid, int>> GetProductCountPerShopAsync();
    }
}
