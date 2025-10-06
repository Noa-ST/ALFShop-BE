using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IShopRepository : IGeneric<Shop>
    {
        Task<IEnumerable<Shop>> GetBySellerIdAsync(string sellerId);

        Task<IEnumerable<Shop>> GetAllActiveAsync(); // IsDeleted == false
    }
}
