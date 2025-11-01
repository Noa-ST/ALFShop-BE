using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IProductRepository : IGeneric<Product>
    {
        Task<IEnumerable<Product>> GetByShopIdAsync(Guid shopId);
        Task<IEnumerable<Product>> GetByGlobalCategoryIdAsync(Guid globalCategoryId);
        Task<Product?> GetDetailByIdAsync(Guid id);
        Task<int> SoftDeleteAsync(Guid id);
        Task<int> AddWithImagesAsync(Product product, IEnumerable<ProductImage>? images);
        
        // ✅ New: Search and filter with pagination
        Task<(IEnumerable<Product> Products, int TotalCount)> SearchAndFilterAsync(
            string? keyword,
            Guid? shopId,
            Guid? categoryId,
            Domain.Enums.ProductStatus? status,
            decimal? minPrice,
            decimal? maxPrice,
            string sortBy,
            string sortOrder,
            int page,
            int pageSize);

        // ✅ New: Stock management
        Task<int> UpdateStockQuantityAsync(Guid productId, int quantityChange);
        Task<Product?> GetByIdForUpdateAsync(Guid id); // For stock update with tracking

        // ✅ New: Count products by category
        Task<int> CountByCategoryIdAsync(Guid categoryId);
        
        // ✅ New: Update product with images in transaction
        Task<int> UpdateWithImagesAsync(Product product, IEnumerable<ProductImage>? newImages);
        
        // ✅ New: Recalculate product rating from reviews
        Task<int> RecalculateRatingAsync(Guid productId);
        
        // ✅ New: Get product statistics
        Task<ProductStatistics> GetProductStatisticsAsync();
    }
    
    // ✅ New: DTO for product statistics
    public class ProductStatistics
    {
        public int TotalProducts { get; set; }
        public int PendingProducts { get; set; }
        public int ApprovedProducts { get; set; }
        public int RejectedProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
