using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using System.Net;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ProductService(
        IProductRepository productRepo,
        IMapper mapper
        , IProductImageRepository productImageRepo
    ) : IProductService
    {
        public async Task<ServiceResponse> RejectProductAsync(Guid productId, string? rejectionReason)
        {
            var product = await productRepo.GetByIdAsync(productId);

            // 1. Kiểm tra tồn tại và IsDeleted
            if (product == null || product.IsDeleted)
            {
                return ServiceResponse.Fail("Không tìm thấy sản phẩm hoặc đã bị xóa.", HttpStatusCode.NotFound);
            }

            // 2. Kiểm tra trạng thái hiện tại (chỉ xử lý nếu đang Approved hoặc Pending)
            if (product.Status != ProductStatus.Pending && product.Status != ProductStatus.Approved)
            {
                return ServiceResponse.Fail($"Sản phẩm không thể bị từ chối từ trạng thái hiện tại: {product.Status}.", HttpStatusCode.BadRequest);
            }


            // 3. Cập nhật trạng thái và Lý do
            product.Status = ProductStatus.Rejected;
            product.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(rejectionReason))
            {
                product.Reason = rejectionReason;
            }
            else
            {
                // Xóa lý do cũ (nếu có) nếu Admin không cung cấp lý do mới
                product.Reason = null;
            }
           

            int result = await productRepo.UpdateAsync(product);

            return result > 0
                ? ServiceResponse.Success("Từ chối sản phẩm thành công. Lý do đã được ghi nhận.")
                : ServiceResponse.Fail("Lỗi cập nhật CSDL khi từ chối sản phẩm.", HttpStatusCode.InternalServerError);
        }
    
    public async Task<ServiceResponse> ApproveProductAsync(Guid productId)
        {
            var product = await productRepo.GetByIdAsync(productId);

            // 1. Kiểm tra tồn tại và IsDeleted
            if (product == null || product.IsDeleted)
            {
                return ServiceResponse.Fail("Không tìm thấy sản phẩm hoặc đã bị xóa.", HttpStatusCode.NotFound);
            }

            // 2. Kiểm tra trạng thái hiện tại (chỉ duyệt nếu đang Pending)
            if (product.Status != ProductStatus.Pending)
            {
                return ServiceResponse.Fail($"Sản phẩm không ở trạng thái chờ duyệt (Pending). Trạng thái hiện tại: {product.Status}.", HttpStatusCode.BadRequest);
            }

            // 3. Cập nhật trạng thái
            product.Status = ProductStatus.Approved;
            product.UpdatedAt = DateTime.UtcNow;

            int result = await productRepo.UpdateAsync(product);

            return result > 0
                ? ServiceResponse.Success("Duyệt sản phẩm thành công.")
                : ServiceResponse.Fail("Lỗi cập nhật CSDL khi duyệt sản phẩm.", HttpStatusCode.InternalServerError);
        }
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