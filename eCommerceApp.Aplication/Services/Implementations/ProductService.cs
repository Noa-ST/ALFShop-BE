using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ProductService(
        IProductRepository productRepo,
        IMapper mapper
        , IProductImageRepository productImageRepo
        , IShopRepository shopRepository
        , IGlobalCategoryRepository globalCategoryRepository
        , IWebHostEnvironment webHostEnvironment
        , IHttpContextAccessor httpContextAccessor
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
            try
            {
                // 0. Validate FK trước khi lưu để tránh lỗi 500
                var shop = await shopRepository.GetByIdAsync(product.ShopId);
                if (shop == null || shop.IsDeleted)
                {
                    return new ServiceResponse(false, "ShopId không hợp lệ hoặc shop đã bị xoá.");
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
                entity.Images = null; // Không set Images trên entity để tránh tracking conflicts

                // ✅ Khai báo outputImages ngoài để sử dụng sau
                List<ProductImage> outputImages = new List<ProductImage>();

                // ✅ Xử lý ảnh: chấp nhận URL hoặc Base64. Nếu base64 -> lưu file về wwwroot/uploads/products
                if (product.ImageUrls != null && product.ImageUrls.Any())
                {
                    var distinctInputs = product.ImageUrls
                        .Where(u => !string.IsNullOrWhiteSpace(u))
                        .Select(u => u.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var usedIds = new HashSet<Guid>(); // ✅ Track các Id đã sử dụng để tránh duplicate
                    
                    // ✅ Dùng IWebHostEnvironment để lấy đường dẫn wwwroot chính xác
                    string webRoot = webHostEnvironment.WebRootPath;
                    if (string.IsNullOrEmpty(webRoot))
                    {
                        webRoot = Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot");
                    }
                    
                    string uploadRoot = Path.Combine(webRoot, "uploads", "products");
                    Directory.CreateDirectory(uploadRoot);

                    foreach (var input in distinctInputs)
                    {
                        try
                        {
                            string finalUrl;

                            bool isHttp = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                          || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                            bool isDataUrl = input.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                                             && input.Contains(";base64,");

                            if (isHttp)
                            {
                                finalUrl = input; // giữ nguyên URL
                            }
                            else
                            {
                                // Xử lý chuỗi base64 (có thể ở dạng data URL hoặc chỉ raw base64)
                                string base64Part = input;
                                string extension = "jpg"; // mặc định an toàn

                                if (isDataUrl)
                                {
                                    // data:image/png;base64,XXXXX
                                    var headerEnd = input.IndexOf(",");
                                    if (headerEnd < 0) 
                                    {
                                        // Skip ảnh không hợp lệ
                                        continue;
                                    }
                                    var header = input.Substring(0, headerEnd);
                                    base64Part = input[(headerEnd + 1)..];
                                    if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase)) extension = "png";
                                    else if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) || header.Contains("image/jpg", StringComparison.OrdinalIgnoreCase)) extension = "jpg";
                                    else if (header.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) extension = "webp";
                                }

                                // Tạo tên file
                                string fileName = $"{Guid.NewGuid():N}.{extension}";
                                string filePath = Path.Combine(uploadRoot, fileName);

                                // Ghi file với error handling
                                try
                                {
                                    byte[] imageBytes = Convert.FromBase64String(base64Part);
                                    if (imageBytes == null || imageBytes.Length == 0)
                                    {
                                        continue; // Skip ảnh không hợp lệ
                                    }
                                    await File.WriteAllBytesAsync(filePath, imageBytes);

                                    // URL tương đối để client truy cập qua static files
                                    finalUrl = $"/uploads/products/{fileName}";
                                }
                                catch (FormatException)
                                {
                                    // Base64 không hợp lệ, skip ảnh này
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    // Log lỗi nhưng tiếp tục với ảnh khác
                                    continue;
                                }
                            }

                            // ✅ Tạo Id duy nhất (đảm bảo không trùng)
                            Guid imageId;
                            do
                            {
                                imageId = Guid.NewGuid();
                            } while (usedIds.Contains(imageId));
                            
                            usedIds.Add(imageId);

                            outputImages.Add(new ProductImage
                            {
                                Id = imageId, // ✅ Id duy nhất đã được đảm bảo
                                Url = finalUrl,
                                CreatedAt = DateTime.UtcNow,
                                IsDeleted = false
                            });
                        }
                        catch (Exception ex)
                        {
                            // Log nhưng tiếp tục xử lý ảnh tiếp theo để không block các ảnh khác
                            continue;
                        }
                    }

                    // ✅ Images đã được thêm vào outputImages, không set trên entity để tránh tracking conflicts
                }

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
            var active = data.Where(p => !p.IsDeleted);
            var products = mapper.Map<IEnumerable<GetProduct>>(active);
            
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
    }
}