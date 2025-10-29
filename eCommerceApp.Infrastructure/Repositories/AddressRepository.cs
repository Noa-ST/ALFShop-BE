// File: eCommerceApp.Infrastructure/Repositories/AddressRepository.cs

using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            // 1. Tìm và hủy đặt mặc định cho tất cả địa chỉ cũ của User
            var currentDefaults = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();

            // Huỷ đặt mặc định
            currentDefaults.ForEach(a => a.IsDefault = false);

            // 2. Đặt địa chỉ mới làm mặc định
            var newDefault = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId && !a.IsDeleted);

            if (newDefault != null)
            {
                newDefault.IsDefault = true;
            }

            // 3. Lưu thay đổi
            await _context.SaveChangesAsync();
        }
    }
}