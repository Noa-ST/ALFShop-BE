using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Net;
using System.Net.Http; // NEW

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ShopService : IShopService
    {
        private readonly IShopRepository _shopRepository;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageStorageService _imageStorage; // NEW
        private readonly HttpClient _httpClient = new HttpClient(); // NEW

        public ShopService(
            IShopRepository shopRepository, 
            IMapper mapper,
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork unitOfWork,
            IImageStorageService imageStorage // NEW
        )
        {
            _shopRepository = shopRepository;
            _mapper = mapper;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _imageStorage = imageStorage; // NEW
        }

        public async Task<ServiceResponse> CreateAsync(CreateShop shop)
        {
            // ✅ Validate SellerId từ JWT (nếu có) hoặc từ DTO
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            // Nếu có userId từ JWT, phải match với SellerId trong DTO
            if (!string.IsNullOrEmpty(userId) && userId != shop.SellerId)
            {
                return ServiceResponse.Fail(
                    "Bạn không có quyền tạo shop cho seller khác.", 
                    HttpStatusCode.Forbidden);
            }

            // ✅ Validate SellerId tồn tại và có role Seller
            var seller = await _userManager.FindByIdAsync(shop.SellerId);
            if (seller == null)
            {
                return ServiceResponse.Fail(
                    "SellerId không tồn tại.", 
                    HttpStatusCode.BadRequest);
            }

            var roles = await _userManager.GetRolesAsync(seller);
            if (!roles.Contains("Seller") && !roles.Contains("Admin"))
            {
                return ServiceResponse.Fail(
                    "Người dùng này không có quyền tạo shop. Chỉ Seller và Admin mới được tạo shop.", 
                    HttpStatusCode.Forbidden);
            }

            // ✅ Kiểm tra seller đã có shop chưa
            var existingShop = await _shopRepository.GetBySellerIdAsync(shop.SellerId);
            if (existingShop.Any())
            {
                return ServiceResponse.Fail(
                    "Seller này đã có shop. Mỗi seller chỉ được tạo 1 shop.", 
                    HttpStatusCode.BadRequest);
            }

            // Map sang entity và gán audit info
            var mappedData = _mapper.Map<Shop>(shop);
            mappedData.CreatedAt = DateTime.UtcNow;
            mappedData.UpdatedAt = null;
            mappedData.IsDeleted = false;

            // NEW: xử lý logo → luôn upload Cloudinary
            if (!string.IsNullOrWhiteSpace(shop.Logo))
            {
                try
                {
                    var input = shop.Logo.Trim();
                    bool isHttp = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                  || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                    string finalUrl;
                    if (isHttp)
                    {
                        var bytes = await _httpClient.GetByteArrayAsync(input);
                        var base64 = Convert.ToBase64String(bytes);
                        var dataUrl = $"data:image/*;base64,{base64}";
                        finalUrl = await _imageStorage.UploadBase64Async(dataUrl, "uploads/shops");
                    }
                    else
                    {
                        finalUrl = await _imageStorage.UploadBase64Async(input, "uploads/shops");
                    }

                    mappedData.Logo = finalUrl;
                }
                catch (Exception ex)
                {
                    return ServiceResponse.Fail($"Upload logo lên Cloudinary thất bại: {ex.Message}", HttpStatusCode.BadRequest);
                }
            }

            await _shopRepository.AddAsync(mappedData);
            int saved = await _unitOfWork.SaveChangesAsync();
            return saved > 0
                ? ServiceResponse.Success("Shop created successfully.")
                : ServiceResponse.Fail("Failed to create shop.", HttpStatusCode.InternalServerError);
        }

        public async Task<ServiceResponse> UpdateAsync(UpdateShop shop, string? userId = null)
        {
            var existing = await _shopRepository.GetByIdAsync(shop.Id);
            if (existing == null || existing.IsDeleted)
            {
                return ServiceResponse.Fail(
                    "Shop not found.", 
                    HttpStatusCode.NotFound);
            }

            // ✅ Validate ownership: Seller chỉ được update shop của mình (trừ Admin)
            if (!string.IsNullOrEmpty(userId))
            {
                var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
                if (!isAdmin && existing.SellerId != userId)
                {
                    return ServiceResponse.Fail(
                        "Bạn không có quyền cập nhật shop này. Chỉ có thể cập nhật shop của chính bạn.", 
                        HttpStatusCode.Forbidden);
                }
            }

            _mapper.Map(shop, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            // NEW: xử lý logo → luôn upload Cloudinary nếu có giá trị mới
            if (!string.IsNullOrWhiteSpace(shop.Logo))
            {
                try
                {
                    var input = shop.Logo.Trim();
                    bool isHttp = input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                  || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                    string finalUrl;
                    if (isHttp)
                    {
                        var bytes = await _httpClient.GetByteArrayAsync(input);
                        var base64 = Convert.ToBase64String(bytes);
                        var dataUrl = $"data:image/*;base64,{base64}";
                        finalUrl = await _imageStorage.UploadBase64Async(dataUrl, "uploads/shops");
                    }
                    else
                    {
                        finalUrl = await _imageStorage.UploadBase64Async(input, "uploads/shops");
                    }

                    existing.Logo = finalUrl;
                }
                catch (Exception ex)
                {
                    return ServiceResponse.Fail($"Upload logo lên Cloudinary thất bại: {ex.Message}", HttpStatusCode.BadRequest);
                }
            }

            await _shopRepository.UpdateAsync(existing);
            int saved = await _unitOfWork.SaveChangesAsync();
            return saved > 0
                ? ServiceResponse.Success("Shop updated successfully.")
                : ServiceResponse.Fail("Failed to update shop.", HttpStatusCode.InternalServerError);
        }

        public async Task<ServiceResponse> DeleteAsync(Guid id, string? userId = null)
        {
            var entity = await _shopRepository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted)
            {
                return ServiceResponse.Fail(
                    "Shop not found.", 
                    HttpStatusCode.NotFound);
            }

            // ✅ Validate ownership: Seller chỉ được delete shop của mình (trừ Admin)
            if (!string.IsNullOrEmpty(userId))
            {
                var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
                if (!isAdmin && entity.SellerId != userId)
                {
                    return ServiceResponse.Fail(
                        "Bạn không có quyền xóa shop này. Chỉ có thể xóa shop của chính bạn.", 
                        HttpStatusCode.Forbidden);
                }
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _shopRepository.UpdateAsync(entity);
            
            // ✅ Fix: Lưu vào database
            int saved = await _unitOfWork.SaveChangesAsync();
            
            return saved > 0
                ? ServiceResponse.Success("Shop deleted (soft delete).")
                : ServiceResponse.Fail("Failed to delete shop.", HttpStatusCode.InternalServerError);
        }

        public async Task<GetShop?> GetByIdAsync(Guid id)
        {
            var shop = await _shopRepository.GetByIdAsync(id);
            return shop == null || shop.IsDeleted ? null : _mapper.Map<GetShop>(shop);
        }

        public async Task<IEnumerable<GetShop>> GetAllActiveAsync()
        {
            // ✅ Fix: Repository đã filter IsDeleted rồi, không cần filter lại
            var shops = await _shopRepository.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<GetShop>>(shops);
        }

        public async Task<IEnumerable<GetShop>> GetBySellerIdAsync(string sellerId)
        {
            // ✅ Fix: Repository đã filter IsDeleted rồi, không cần filter lại
            var shops = await _shopRepository.GetBySellerIdAsync(sellerId);
            return _mapper.Map<IEnumerable<GetShop>>(shops);
        }

        // ✅ New: Search and filter with pagination
        public async Task<PagedResult<GetShop>> SearchAndFilterAsync(ShopFilterDto filter)
        {
            // Validate filter
            filter.Validate();

            // Call repository
            var (shops, totalCount) = await _shopRepository.SearchAndFilterAsync(
                filter.Keyword,
                filter.City,
                filter.Country,
                filter.MinRating,
                filter.MaxRating,
                filter.SortBy,
                filter.SortOrder,
                filter.Page,
                filter.PageSize);

            // Map to DTOs
            var shopDtos = _mapper.Map<IEnumerable<GetShop>>(shops);

            return new PagedResult<GetShop>
            {
                Data = shopDtos.ToList(),
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        // ✅ New: Rating management - Tính rating từ reviews của tất cả products trong shop
        public async Task<ServiceResponse> RecalculateRatingAsync(Guid shopId)
        {
            var shop = await _shopRepository.GetByIdForUpdateAsync(shopId);
            if (shop == null)
            {
                return ServiceResponse.Fail("Shop not found.", HttpStatusCode.NotFound);
            }

            // ✅ Sử dụng repository method thay vì AppDbContext trực tiếp
            int result = await _shopRepository.RecalculateRatingAsync(shopId);

            if (result > 0)
            {
                // Reload shop để lấy rating đã cập nhật
                var updatedShop = await _shopRepository.GetByIdAsync(shopId);
                if (updatedShop != null)
                {
                    return ServiceResponse.Success($"Đã tính lại rating: {updatedShop.AverageRating:F2} ({updatedShop.ReviewCount} reviews).");
                }
            }

            return ServiceResponse.Fail("Failed to recalculate rating.", HttpStatusCode.InternalServerError);
        }

        // ✅ New: Statistics for Admin dashboard
        public async Task<ServiceResponse<object>> GetStatisticsAsync()
        {
            try
            {
                var totalShops = await _shopRepository.GetTotalCountAsync();
                var shopsByCity = await _shopRepository.GetShopsByCityAsync();
                var productCountPerShop = await _shopRepository.GetProductCountPerShopAsync();
                
                // Get all shops for calculations
                var allShops = await _shopRepository.GetAllActiveAsync();
                
                // Calculate statistics
                var shopsWithoutProducts = allShops
                    .Where(s => !productCountPerShop.ContainsKey(s.Id))
                    .Count();

                var averageRating = allShops.Any()
                    ? allShops.Average(s => s.AverageRating)
                    : 0.0f;

                var topRatedShops = allShops
                    .OrderByDescending(s => s.AverageRating)
                    .ThenByDescending(s => s.ReviewCount)
                    .Take(5)
                    .Select(s => new
                    {
                        ShopId = s.Id,
                        ShopName = s.Name,
                        Rating = s.AverageRating,
                        ReviewCount = s.ReviewCount,
                        City = s.City
                    })
                    .ToList();

                var totalProducts = productCountPerShop.Values.Sum();
                var averageProductsPerShop = productCountPerShop.Any()
                    ? productCountPerShop.Values.Average()
                    : 0;

                var statistics = new
                {
                    TotalShops = totalShops,
                    ShopsByCity = shopsByCity,
                    ShopsWithoutProducts = shopsWithoutProducts,
                    AverageRating = Math.Round(averageRating, 2),
                    TotalProducts = totalProducts,
                    AverageProductsPerShop = Math.Round(averageProductsPerShop, 2),
                    TopRatedShops = topRatedShops
                };

                return ServiceResponse<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResponse<object>.Fail(
                    $"Lỗi khi lấy thống kê: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }
    }
}
