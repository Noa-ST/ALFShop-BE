using eCommerceApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IAddressRepository : IGeneric<Address>
    {
        Task<IEnumerable<Address>> GetUserAddressesAsync(string userId);
        Task SetDefaultAddressAsync(string userId, Guid addressId);
        
        // ✅ New: Chỉ hủy default của tất cả items (không set mới)
        Task UnsetAllDefaultsAsync(string userId);
        
        // ✅ New: Tự động set address đầu tiên làm default
        Task<bool> SetFirstAddressAsDefaultAsync(string userId);
        
        // ✅ New: Lấy default address của user
        Task<Address?> GetDefaultAddressAsync(string userId);
        
        // ✅ New: Check duplicate address
        Task<bool> IsDuplicateAddressAsync(string userId, string fullStreet, string ward, string district, string province, Guid? excludeId = null);
        
        // ✅ New: Đếm số lượng address của user
        Task<int> CountUserAddressesAsync(string userId);
    }
}