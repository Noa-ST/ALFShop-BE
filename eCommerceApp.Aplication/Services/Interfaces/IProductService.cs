using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<GetProduct>> GetAllAsync();
        Task<IEnumerable<GetProduct>> GetByShopIdAsync(Guid shopId);
        Task<IEnumerable<GetProduct>> GetByGlobalCategoryIdAsync(Guid globalCategoryId);
        Task<GetProductDetail?> GetDetailByIdAsync(Guid id);
        Task<ServiceResponse> AddAsync(CreateProduct product, string userId);
        Task<ServiceResponse> UpdateAsync(Guid id, UpdateProduct product, string userId);
        Task<ServiceResponse> DeleteAsync(Guid id, string userId);

        Task<ServiceResponse> ApproveProductAsync(Guid productId);
        Task<ServiceResponse> RejectProductAsync(Guid productId, string? rejectionReason);

        // ✅ New: Search and filter with pagination
        Task<PagedResult<GetProduct>> SearchAndFilterAsync(ProductFilterDto filter);

        // ✅ New: Stock management
        Task<ServiceResponse> ReduceStockAsync(Guid productId, int quantity);
        Task<ServiceResponse> RestoreStockAsync(Guid productId, int quantity);
        Task<ServiceResponse> UpdateStockQuantityAsync(Guid productId, int newQuantity, string userId);

        // ✅ New: Rating management
        Task<ServiceResponse> RecalculateRatingAsync(Guid productId);

        // ✅ New: Admin features
        Task<PagedResult<GetProduct>> GetProductsByStatusAsync(ProductStatus status, int page, int pageSize);
        Task<object> GetProductStatisticsAsync();
    }
}
