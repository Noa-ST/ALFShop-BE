using eCommerceApp.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IAddressRepository : IGeneric<Address>
    {
        Task<IEnumerable<Address>> GetUserAddressesAsync(string userId);
        Task SetDefaultAddressAsync(string userId, Guid addressId);
    }
}