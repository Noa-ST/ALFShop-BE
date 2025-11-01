// File: eCommerceApp.Infrastructure/Repositories/AddressRepository.cs

using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class AddressRepository : GenericRepository<Address>, IAddressRepository
    {
        private readonly AppDbContext _context;

        public AddressRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Address>> GetUserAddressesAsync(string userId)
        {
            // Lấy tất cả địa chỉ của User, sắp xếp mặc định lên đầu và mới nhất
            return await _context.Addresses
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .OrderByDescending(a => a.IsDefault) // Địa chỉ mặc định (true) lên đầu
                .ThenByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task SetDefaultAddressAsync(string userId, Guid addressId)
        {
            // ✅ Fix: Validate addressId không được Guid.Empty
            if (addressId == Guid.Empty)
            {
                throw new ArgumentException("AddressId cannot be Guid.Empty", nameof(addressId));
            }

            // 1. Tìm và hủy đặt mặc định cho tất cả địa chỉ cũ của User
            var currentDefaults = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault && !a.IsDeleted)
                .ToListAsync();

            // Huỷ đặt mặc định
            foreach (var addr in currentDefaults)
            {
                addr.IsDefault = false;
            }

            // 2. Đặt địa chỉ mới làm mặc định
            var newDefault = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId && !a.IsDeleted);

            if (newDefault != null)
            {
                newDefault.IsDefault = true;
            }
            else
            {
                throw new InvalidOperationException($"Address with Id {addressId} not found or already deleted.");
            }

            // 3. Lưu thay đổi
            await _context.SaveChangesAsync();
        }

        // ✅ New: Chỉ hủy default của tất cả items (không set mới)
        public async Task UnsetAllDefaultsAsync(string userId)
        {
            var currentDefaults = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault && !a.IsDeleted)
                .ToListAsync();

            foreach (var addr in currentDefaults)
            {
                addr.IsDefault = false;
            }

            if (currentDefaults.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        // ✅ New: Tự động set address đầu tiên làm default
        public async Task<bool> SetFirstAddressAsDefaultAsync(string userId)
        {
            var firstAddress = await _context.Addresses
                .Where(a => a.UserId == userId && !a.IsDeleted && !a.IsDefault)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (firstAddress != null)
            {
                firstAddress.IsDefault = true;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        // ✅ New: Lấy default address của user
        public async Task<Address?> GetDefaultAddressAsync(string userId)
        {
            return await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault && !a.IsDeleted)
                .FirstOrDefaultAsync();
        }

        // ✅ New: Check duplicate address
        public async Task<bool> IsDuplicateAddressAsync(string userId, string fullStreet, string ward, string district, string province, Guid? excludeId = null)
        {
            var query = _context.Addresses
                .Where(a => a.UserId == userId 
                    && !a.IsDeleted
                    && a.FullStreet.ToLower().Trim() == fullStreet.ToLower().Trim()
                    && a.Ward.ToLower().Trim() == ward.ToLower().Trim()
                    && a.District.ToLower().Trim() == district.ToLower().Trim()
                    && a.Province.ToLower().Trim() == province.ToLower().Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(a => a.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        // ✅ New: Đếm số lượng address của user
        public async Task<int> CountUserAddressesAsync(string userId)
        {
            return await _context.Addresses
                .CountAsync(a => a.UserId == userId && !a.IsDeleted);
        }
    }
}