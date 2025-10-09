using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IProductImageRepository : IGeneric<ProductImage>
    {
        Task<IEnumerable<ProductImage>> GetByProductIdAsync(Guid productId);
        Task<int> SoftDeleteByProductIdAsync(Guid productId);
        Task AddRangeAsync(IEnumerable<ProductImage> images);
    }
}
