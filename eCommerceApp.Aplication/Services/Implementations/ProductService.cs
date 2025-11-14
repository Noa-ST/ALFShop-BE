using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Http;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ProductService(
        IProductRepository productRepo,
        IMapper mapper
        , IShopRepository shopRepository
        , IGlobalCategoryRepository globalCategoryRepository
        , IHttpContextAccessor httpContextAccessor
        , IImageStorageService imageStorage // NEW: sử dụng Cloudinary thông qua abstraction
    ) : IProductService
    {
        private readonly HttpClient _httpClient = new HttpClient();
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


            int result = await productRepo.UpdateStatusAsync(product);

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

            int result = await productRepo.UpdateStatusAsync(product);

            return result > 0
                ? ServiceResponse.Success("Duyệt sản phẩm thành công.")
                : ServiceResponse.Fail("Lỗi cập nhật CSDL khi duyệt sản phẩm.", HttpStatusCode.InternalServerError);
        }
        public async Task<ServiceResponse> AddAsync(CreateProduct product, string userId)
        {
            try
            {
                // ✅ Fix: Validate Shop ownership
                var shop = await shopRepository.GetByIdAsync(product.ShopId);
                if (shop == null || shop.IsDeleted)
                {
                    return ServiceResponse.Fail("ShopId không hợp lệ hoặc shop đã bị xoá.", HttpStatusCode.BadRequest);
                }

                // ✅ Kiểm tra shop thuộc về user hiện tại (trừ Admin)
                var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
                if (!isAdmin && shop.SellerId != userId)
                {
                    return ServiceResponse.Fail("Bạn không có quyền tạo sản phẩm cho shop này.", HttpStatusCode.Forbidden);
                }

                var category = await globalCategoryRepository.GetByIdAsync(product.CategoryId);
                if (category == null)
                {
                    return new ServiceResponse(false, "CategoryId (GlobalCategory) không tồn tại.");
                }

                var entity = mapper.Map<Product>(product);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = null;
                entity.IsDeleted = false;

                // 🔧 Đảm bảo không bị nhân đôi ảnh (xoá bộ ảnh mà AutoMapper đã map sẵn)
                entity.Images = new List<ProductImage>(); // Set danh sách rỗng để phù hợp với thuộc tính non-nullable

                // ✅ Khai báo outputImages ngoài để sử dụng sau
                List<ProductImage> outputImages = new List<ProductImage>();

                // ✅ Xử lý ảnh: chuẩn hoá sang Cloudinary (100%)
                if (product.ImageUrls != null && product.ImageUrls.Any())
                {
                    var distinctInputs = product.ImageUrls
                        .Where(u => !string.IsNullOrWhiteSpace(u))
                        .Select(u => u.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var input in distinctInputs)
                    {
                        try
                        {
                            string finalUrl;

                            bool isHttp = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                          || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                            if (isHttp)
                            {
                                // Tải dữ liệu từ URL và re-upload lên Cloudinary
                                var bytes = await _httpClient.GetByteArrayAsync(input);
                                var base64 = Convert.ToBase64String(bytes);
                                var dataUrl = $"data:image/*;base64,{base64}";
                                finalUrl = await imageStorage.UploadBase64Async(dataUrl, "uploads/products");
                            }
                            else
                            {
                                // Base64 hoặc data URL → upload lên Cloudinary
                                finalUrl = await imageStorage.UploadBase64Async(input, "uploads/products");
                            }

                            outputImages.Add(new ProductImage
                            {
                                Id = Guid.NewGuid(),
                                Url = finalUrl,
                                CreatedAt = DateTime.UtcNow,
                                IsDeleted = false
                            });
                        }
                        catch
                        {
                            // Skip ảnh lỗi, tiếp tục ảnh khác
                            continue;
                        }
                    }
                }

                // ✅ Images đã được thêm vào outputImages, không set trên entity để tránh tracking conflicts

                // ✅ Gọi repo lưu 1 lần duy nhất, truyền images riêng
                // Log số lượng ảnh để debug
                if (outputImages.Count == 0 && product.ImageUrls != null && product.ImageUrls.Any())
                {
                    return new ServiceResponse(false, "Không có ảnh nào được xử lý thành công. Vui lòng kiểm tra định dạng ảnh.");
                }

                int result = await productRepo.AddWithImagesAsync(entity, outputImages);

                if (result > 0)
                {
                    string message = outputImages.Count > 0
                        ? $"Product created successfully with {outputImages.Count} image(s)."
                        : "Product created successfully.";
                    return new ServiceResponse(true, message);
                }

                return new ServiceResponse(false, "Failed to create product.");
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết để debug
                return new ServiceResponse(false, $"Error creating product: {ex.Message}");
            }
        }


        public async Task<ServiceResponse> UpdateAsync(Guid id, UpdateProduct product, string userId)
        {
            var existing = await productRepo.GetDetailByIdAsync(id);
            if (existing == null || existing.IsDeleted)
                return ServiceResponse.Fail("Product not found.", HttpStatusCode.NotFound);

            // ✅ Fix: Validate Shop ownership
            var shop = await shopRepository.GetByIdAsync(existing.ShopId);
            if (shop == null)
                return ServiceResponse.Fail("Shop not found.", HttpStatusCode.NotFound);

            var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
            if (!isAdmin && shop.SellerId != userId)
            {
                return ServiceResponse.Fail("Bạn không có quyền cập nhật sản phẩm này.", HttpStatusCode.Forbidden);
            }

            // ✅ Validate CategoryId nếu có thay đổi
            if (product.CategoryId != Guid.Empty && product.CategoryId != existing.GlobalCategoryId)
            {
                var category = await globalCategoryRepository.GetByIdAsync(product.CategoryId);
                if (category == null)
                    return ServiceResponse.Fail("CategoryId không tồn tại.", HttpStatusCode.BadRequest);
            }

            mapper.Map(product, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            // ✅ Fix: Sử dụng repository method để đảm bảo atomicity khi update images
            try
            {
                IEnumerable<ProductImage>? newImages = null;
                if (product.ImageUrls != null && product.ImageUrls.Any())
                {
                    var distinctInputs = product.ImageUrls
                        .Where(u => !string.IsNullOrWhiteSpace(u))
                        .Select(u => u.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var imgs = new List<ProductImage>();

                    foreach (var input in distinctInputs)
                    {
                        try
                        {
                            string finalUrl;

                            bool isHttp = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                          || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                            if (isHttp)
                            {
                                // Tải dữ liệu từ URL và re-upload lên Cloudinary
                                var bytes = await _httpClient.GetByteArrayAsync(input);
                                var base64 = Convert.ToBase64String(bytes);
                                var dataUrl = $"data:image/*;base64,{base64}";
                                finalUrl = await imageStorage.UploadBase64Async(dataUrl, "uploads/products");
                            }
                            else
                            {
                                finalUrl = await imageStorage.UploadBase64Async(input, "uploads/products");
                            }

                            imgs.Add(new ProductImage
                            {
                                Id = Guid.NewGuid(),
                                ProductId = existing.Id,
                                Url = finalUrl,
                                CreatedAt = DateTime.UtcNow,
                                IsDeleted = false
                            });
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    newImages = imgs;
                }

                // Update product with images using repository transaction method
                int result = await productRepo.UpdateWithImagesAsync(existing, newImages);

                return result > 0
                    ? ServiceResponse.Success("Product updated successfully.")
                    : ServiceResponse.Fail("Failed to update product.", HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Fail($"Error updating product: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }


        public async Task<ServiceResponse> DeleteAsync(Guid id, string userId)
        {
            var existing = await productRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted)
                return ServiceResponse.Fail("Product not found.", HttpStatusCode.NotFound);

            // ✅ Fix: Validate Shop ownership
            var shop = await shopRepository.GetByIdAsync(existing.ShopId);
            if (shop == null)
                return ServiceResponse.Fail("Shop not found.", HttpStatusCode.NotFound);

            var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
            if (!isAdmin && shop.SellerId != userId)
            {
                return ServiceResponse.Fail("Bạn không có quyền xóa sản phẩm này.", HttpStatusCode.Forbidden);
            }

            int result = await productRepo.SoftDeleteAsync(id);
            return result > 0
                ? ServiceResponse.Success("Product deleted (soft delete).")
                : ServiceResponse.Fail("Failed to delete product.", HttpStatusCode.InternalServerError);
        }

        // ✅ Helper method để chuyển relative URL thành full URL
        private string GetFullImageUrl(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) return relativeUrl ?? string.Empty;

            // Nếu đã là full URL thì giữ nguyên
            if (relativeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return relativeUrl;

            // Lấy base URL từ HttpContext
            var request = httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                return $"{baseUrl}{relativeUrl}";
            }

            // Fallback cho development
            return $"https://localhost:7109{relativeUrl}";
        }

        // ✅ Helper để convert ProductImage thành ProductImageDto với full URL
        private ProductImageDto MapToProductImageDto(ProductImage image)
        {
            return new ProductImageDto
            {
                Id = image.Id,
                Url = GetFullImageUrl(image.Url)
            };
        }

        public async Task<IEnumerable<GetProduct>> GetAllAsync()
        {
            var data = await productRepo.GetAllAsync();
            // ✅ Fix: Xóa duplicate filter - Repository đã filter IsDeleted = false rồi
            var products = mapper.Map<IEnumerable<GetProduct>>(data);

            // ✅ Convert relative URLs thành full URLs cho tất cả ảnh
            foreach (var product in products)
            {
                if (product.ProductImages != null && product.ProductImages.Any())
                {
                    product.ProductImages = product.ProductImages.Select(img => new ProductImageDto
                    {
                        Id = img.Id,
                        Url = GetFullImageUrl(img.Url)
                    }).ToList();
                }
            }

            return products;
        }

        public async Task<IEnumerable<GetProduct>> GetByShopIdAsync(Guid shopId)
        {
            var data = await productRepo.GetByShopIdAsync(shopId);
            var products = mapper.Map<IEnumerable<GetProduct>>(data);

            // ✅ Convert relative URLs thành full URLs cho tất cả ảnh
            foreach (var product in products)
            {
                if (product.ProductImages != null && product.ProductImages.Any())
                {
                    product.ProductImages = product.ProductImages.Select(img => new ProductImageDto
                    {
                        Id = img.Id,
                        Url = GetFullImageUrl(img.Url)
                    }).ToList();
                }
            }

            return products;
        }

        // ✅ [ĐÃ SỬA]: Triển khai phương thức mới GetByGlobalCategoryIdAsync
        public async Task<IEnumerable<GetProduct>> GetByGlobalCategoryIdAsync(Guid globalCategoryId)
        {
            // Gọi phương thức mới trong Repository
            var data = await productRepo.GetByGlobalCategoryIdAsync(globalCategoryId);
            var products = mapper.Map<IEnumerable<GetProduct>>(data);

            // ✅ Convert relative URLs thành full URLs cho tất cả ảnh
            foreach (var product in products)
            {
                if (product.ProductImages != null && product.ProductImages.Any())
                {
                    product.ProductImages = product.ProductImages.Select(img => new ProductImageDto
                    {
                        Id = img.Id,
                        Url = GetFullImageUrl(img.Url)
                    }).ToList();
                }
            }

            return products;
        }

        public async Task<GetProductDetail?> GetDetailByIdAsync(Guid id)
        {
            // Bước 1: Repository tải Entity Product kèm theo Shop và Images
            var entity = await productRepo.GetDetailByIdAsync(id);

            if (entity == null || entity.IsDeleted)
                return null;

            // Bước 2: Dùng Mapper chuyển Entity sang DTO
            var productDetail = mapper.Map<GetProductDetail>(entity);

            // ✅ Convert relative URLs thành full URLs cho tất cả ảnh
            if (productDetail.ProductImages != null && productDetail.ProductImages.Any())
            {
                productDetail.ProductImages = productDetail.ProductImages.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    Url = GetFullImageUrl(img.Url)
                }).ToList();
            }

            return productDetail;
        }

        // ✅ New: Search and filter with pagination
        public async Task<PagedResult<GetProduct>> SearchAndFilterAsync(ProductFilterDto filter)
        {
            // Validate filter
            filter.Validate();

            // ✅ Fix: Chỉ hiển thị Approved products trong public search (trừ khi admin override)
            var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
            if (!isAdmin && !filter.Status.HasValue)
            {
                filter.Status = ProductStatus.Approved; // Force Approved status for public users
            }

            // Call repository
            var (products, totalCount) = await productRepo.SearchAndFilterAsync(
                filter.Keyword,
                filter.ShopId,
                filter.CategoryId,
                filter.Status,
                filter.MinPrice,
                filter.MaxPrice,
                filter.SortBy,
                filter.SortOrder,
                filter.Page,
                filter.PageSize);

            // Map to DTOs
            var productDtos = mapper.Map<IEnumerable<GetProduct>>(products);

            // ✅ Convert relative URLs thành full URLs cho tất cả ảnh
            foreach (var product in productDtos)
            {
                if (product.ProductImages != null && product.ProductImages.Any())
                {
                    product.ProductImages = product.ProductImages.Select(img => new ProductImageDto
                    {
                        Id = img.Id,
                        Url = GetFullImageUrl(img.Url)
                    }).ToList();
                }
            }

            return new PagedResult<GetProduct>
            {
                Data = productDtos.ToList(),
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        // ✅ New: Stock management
        public async Task<ServiceResponse> ReduceStockAsync(Guid productId, int quantity)
        {
            if (quantity <= 0)
                return ServiceResponse.Fail("Số lượng phải lớn hơn 0.", HttpStatusCode.BadRequest);

            var product = await productRepo.GetByIdForUpdateAsync(productId);
            if (product == null)
                return ServiceResponse.Fail("Product not found.", HttpStatusCode.NotFound);

            if (product.StockQuantity < quantity)
                return ServiceResponse.Fail($"Không đủ tồn kho. Hiện có: {product.StockQuantity}, yêu cầu: {quantity}", HttpStatusCode.BadRequest);

            int result = await productRepo.UpdateStockQuantityAsync(productId, -quantity);
            return result > 0
                ? ServiceResponse.Success($"Đã giảm {quantity} sản phẩm khỏi tồn kho.")
                : ServiceResponse.Fail("Failed to reduce stock.", HttpStatusCode.InternalServerError);
        }

        public async Task<ServiceResponse> RestoreStockAsync(Guid productId, int quantity)
        {
            if (quantity <= 0)
                return ServiceResponse.Fail("Số lượng phải lớn hơn 0.", HttpStatusCode.BadRequest);

            var product = await productRepo.GetByIdForUpdateAsync(productId);
            if (product == null)
                return ServiceResponse.Fail("Product not found.", HttpStatusCode.NotFound);

            int result = await productRepo.UpdateStockQuantityAsync(productId, quantity);
            return result > 0
                ? ServiceResponse.Success($"Đã hoàn trả {quantity} sản phẩm vào tồn kho.")
                : ServiceResponse.Fail("Failed to restore stock.", HttpStatusCode.InternalServerError);
        }

        public async Task<ServiceResponse> UpdateStockQuantityAsync(Guid productId, int newQuantity, string userId)
        {
            if (newQuantity < 0)
                return ServiceResponse.Fail("Số lượng tồn kho không thể âm.", HttpStatusCode.BadRequest);

            var product = await productRepo.GetByIdForUpdateAsync(productId);
            if (product == null)
                return ServiceResponse.Fail("Product not found.", HttpStatusCode.NotFound);

            // Validate shop ownership
            var shop = await shopRepository.GetByIdAsync(product.ShopId);
            if (shop == null)
                return ServiceResponse.Fail("Shop not found.", HttpStatusCode.NotFound);

            var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
            if (!isAdmin && shop.SellerId != userId)
            {
                return ServiceResponse.Fail("Bạn không có quyền cập nhật tồn kho sản phẩm này.", HttpStatusCode.Forbidden);
            }

            int quantityChange = newQuantity - product.StockQuantity;
            int result = await productRepo.UpdateStockQuantityAsync(productId, quantityChange);
            return result > 0
                ? ServiceResponse.Success($"Đã cập nhật tồn kho thành {newQuantity}.")
                : ServiceResponse.Fail("Failed to update stock.", HttpStatusCode.InternalServerError);
        }

        // ✅ New: Rating management
        public async Task<ServiceResponse> RecalculateRatingAsync(Guid productId)
        {
            var product = await productRepo.GetByIdForUpdateAsync(productId);
            if (product == null)
                return ServiceResponse.Fail("Product not found.", HttpStatusCode.NotFound);

            // ✅ Use repository method to recalculate rating
            int result = await productRepo.RecalculateRatingAsync(productId);

            if (result > 0)
            {
                // Reload product to get updated rating
                var updatedProduct = await productRepo.GetByIdAsync(productId);
                if (updatedProduct != null)
                {
                    return ServiceResponse.Success($"Đã tính lại rating: {updatedProduct.AverageRating:F2} ({updatedProduct.ReviewCount} reviews).");
                }
            }

            return ServiceResponse.Fail("Failed to recalculate rating.", HttpStatusCode.InternalServerError);
        }

        // ✅ New: Admin features
        public async Task<PagedResult<GetProduct>> GetProductsByStatusAsync(ProductStatus status, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var filter = new ProductFilterDto
            {
                Status = status,
                Page = page,
                PageSize = pageSize,
                SortBy = "createdAt",
                SortOrder = "desc"
            };

            return await SearchAndFilterAsync(filter);
        }

        public async Task<object> GetProductStatisticsAsync()
        {
            // ✅ Use repository method to get statistics
            var statistics = await productRepo.GetProductStatisticsAsync();

            return new
            {
                TotalProducts = statistics.TotalProducts,
                PendingProducts = statistics.PendingProducts,
                ApprovedProducts = statistics.ApprovedProducts,
                RejectedProducts = statistics.RejectedProducts,
                OutOfStockProducts = statistics.OutOfStockProducts,
                LowStockProducts = statistics.LowStockProducts,
                TotalRevenue = statistics.TotalRevenue
            };
        }
    }
}