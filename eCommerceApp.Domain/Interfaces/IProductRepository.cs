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
    }
}
