using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ShopService(IShopRepository shopRepository, IMapper mapper) : IShopService
    {
        public async Task<ServiceResponse> CreateAsync(CreateShop shop)
        {
            // 1️⃣ Kiểm tra seller đã có shop chưa
            var existingShop = await shopRepository.GetBySellerIdAsync(shop.SellerId);
            if (existingShop.Any())
                return new ServiceResponse(false, "This seller already has a shop.");

            // Map sang entity và gán audit info
            var mappedData = mapper.Map<Shop>(shop);
            mappedData.CreatedAt = DateTime.UtcNow;
            mappedData.UpdatedAt = null;
            mappedData.IsDeleted = false;

            // Thêm vào DB
            int result = await shopRepository.AddAsync(mappedData);
            return result > 0
                ? new ServiceResponse(true, "Shop created successfully.")
                : new ServiceResponse(false, "Failed to create shop.");
        }

        public async Task<ServiceResponse> UpdateAsync(UpdateShop shop)
        {
            var existing = await shopRepository.GetByIdAsync(shop.Id);
            if (existing == null || existing.IsDeleted)
                return new ServiceResponse(false, "Shop not found.");

            mapper.Map(shop, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            int result = await shopRepository.UpdateAsync(existing);
            return result > 0
                ? new ServiceResponse(true, "Shop updated successfully.")
                : new ServiceResponse(false, "Failed to update shop.");
        }

        public async Task<ServiceResponse> DeleteAsync(Guid id)
        {
            var entity = await shopRepository.GetByIdAsync(id);
            if (entity == null)
                return new ServiceResponse(false, "Shop not found.");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            int result = await shopRepository.UpdateAsync(entity);
            return result > 0
                ? new ServiceResponse(true, "Shop deleted (soft delete).")
                : new ServiceResponse(false, "Failed to delete shop.");
        }

        public async Task<GetShop?> GetByIdAsync(Guid id)
        {
            var shop = await shopRepository.GetByIdAsync(id);
            return shop == null || shop.IsDeleted ? null : mapper.Map<GetShop>(shop);
        }

        public async Task<IEnumerable<GetShop>> GetAllActiveAsync()
        {
            var shops = await shopRepository.GetAllActiveAsync();
            var active = shops.Where(s => !s.IsDeleted);
            return mapper.Map<IEnumerable<GetShop>>(active);
        }

        public async Task<IEnumerable<GetShop>> GetBySellerIdAsync(string sellerId)
        {
            var shops = await shopRepository.GetBySellerIdAsync(sellerId);
            var active = shops.Where(s => !s.IsDeleted);
            return mapper.Map<IEnumerable<GetShop>>(active);
        }
    }
}
