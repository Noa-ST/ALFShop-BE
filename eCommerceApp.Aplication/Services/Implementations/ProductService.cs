using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ProductService(
        IProductRepository productRepo,
        IMapper mapper
        , IProductImageRepository productImageRepo
    ) : IProductService
    {
        public async Task<ServiceResponse> AddAsync(CreateProduct product)
        {
            var entity = mapper.Map<Product>(product);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = null;
            entity.IsDeleted = false;

            // 🔧 Đảm bảo không bị nhân đôi ảnh (xoá bộ ảnh mà AutoMapper đã map sẵn)
            entity.Images?.Clear(); // ✅ Sửa Images thành ProductImages (nếu entity dùng tên này)

            // ✅ Thêm ảnh vào entity trực tiếp
            if (product.ImageUrls != null && product.ImageUrls.Any())
            {
                entity.Images = product.ImageUrls.Select(url => new ProductImage
                {
                    Id = Guid.NewGuid(),
                    // ProductId sẽ được EF Core tự động gán sau khi AddAsync
                    Url = url,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }).ToList();
            }

            // ✅ Gọi repo lưu 1 lần duy nhất
            int result = await productRepo.AddWithImagesAsync(entity, entity.Images);

            return result > 0
                ? new ServiceResponse(true, "Product created successfully.")
                : new ServiceResponse(false, "Failed to create product.");
        }


        public async Task<ServiceResponse> UpdateAsync(Guid id, UpdateProduct product)
        {
            var existing = await productRepo.GetDetailByIdAsync(id);
            if (existing == null || existing.IsDeleted)
                return new ServiceResponse(false, "Product not found.");

            mapper.Map(product, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            int result = await productRepo.UpdateAsync(existing);

            // Cập nhật ảnh nếu có
            if (product.ImageUrls != null && product.ImageUrls.Any())
            {
                if (existing.Images != null) 
                {
                    // Xóa các ảnh cũ
                    foreach (var old in existing.Images) 
                        await productImageRepo.DeleteAsync(old.Id);
                }

                // Thêm các ảnh mới
                foreach (var url in product.ImageUrls)
                {
                    var img = new ProductImage
                    {
                        Id = Guid.NewGuid(),
                        ProductId = existing.Id,
                        Url = url,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    await productImageRepo.AddAsync(img);
                }
            }

            return result > 0
                ? new ServiceResponse(true, "Product updated successfully.")
                : new ServiceResponse(false, "Failed to update product.");
        }


        public async Task<ServiceResponse> DeleteAsync(Guid id)
        {
            int result = await productRepo.SoftDeleteAsync(id);
            return result > 0
                ? new ServiceResponse(true, "Product deleted (soft delete).")
                : new ServiceResponse(false, "Product not found or failed to delete.");
        }

        public async Task<IEnumerable<GetProduct>> GetAllAsync()
        {
            var data = await productRepo.GetAllAsync();
            var active = data.Where(p => !p.IsDeleted);
            return mapper.Map<IEnumerable<GetProduct>>(active);
        }

        public async Task<IEnumerable<GetProduct>> GetByShopIdAsync(Guid shopId)
        {
            var data = await productRepo.GetByShopIdAsync(shopId);
            return mapper.Map<IEnumerable<GetProduct>>(data);
        }

        // ✅ [ĐÃ SỬA]: Triển khai phương thức mới GetByGlobalCategoryIdAsync
        public async Task<IEnumerable<GetProduct>> GetByGlobalCategoryIdAsync(Guid globalCategoryId)
        {
            // Gọi phương thức mới trong Repository
            var data = await productRepo.GetByGlobalCategoryIdAsync(globalCategoryId);
            return mapper.Map<IEnumerable<GetProduct>>(data);
        }

        public async Task<GetProductDetail?> GetDetailByIdAsync(Guid id)
        {
            // Bước 1: Repository tải Entity Product kèm theo Shop và Images
            var entity = await productRepo.GetDetailByIdAsync(id);

            // Bước 2: Dùng Mapper chuyển Entity sang DTO
            return entity == null || entity.IsDeleted
                ? null
                : mapper.Map<GetProductDetail>(entity);
        }
    }
}