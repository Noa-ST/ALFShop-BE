// File: eCommerceApp.Aplication/Services/Implementations/GlobalCategoryService.cs

using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.GlobalCategory;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using System.Net;
using System.Linq;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class GlobalCategoryService : IGlobalCategoryService
    {
        private readonly IGlobalCategoryRepository _globalCategoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public GlobalCategoryService(
            IGlobalCategoryRepository globalCategoryRepository, 
            IProductRepository productRepository,
            IMapper mapper)
        {
            _globalCategoryRepository = globalCategoryRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<GetGlobalCategory>> CreateGlobalCategoryAsync(CreateGlobalCategory dto)
        {
            // ✅ Kiểm tra ParentId có tồn tại không (nếu được cung cấp)
            if (dto.ParentId.HasValue)
            {
                var parent = await _globalCategoryRepository.GetByIdAsync(dto.ParentId.Value);
                if (parent == null || parent.IsDeleted)
                {
                    return ServiceResponse<GetGlobalCategory>.Fail(
                        "ParentId không hợp lệ hoặc không tồn tại.", 
                        HttpStatusCode.BadRequest);
                }
            }

            // ✅ New: Kiểm tra duplicate name trong cùng level
            bool nameExists = await _globalCategoryRepository.ExistsByNameInSameLevelAsync(
                dto.Name, 
                dto.ParentId);
            
            if (nameExists)
            {
                var levelDescription = dto.ParentId.HasValue 
                    ? "cùng danh mục cha" 
                    : "cấp gốc";
                return ServiceResponse<GetGlobalCategory>.Fail(
                    $"Tên danh mục '{dto.Name}' đã tồn tại ở {levelDescription}. Vui lòng chọn tên khác.", 
                    HttpStatusCode.BadRequest);
            }

            var category = _mapper.Map<GlobalCategory>(dto);
            category.CreatedAt = DateTime.UtcNow;
            category.IsDeleted = false;
            
            await _globalCategoryRepository.AddAsync(category);

            var categoryDto = _mapper.Map<GetGlobalCategory>(category);
            return ServiceResponse<GetGlobalCategory>.Success(categoryDto, "Tạo danh mục toàn cầu thành công.");
        }


        public async Task<ServiceResponse<bool>> UpdateGlobalCategoryAsync(Guid id, UpdateGlobalCategory dto)
        {
            var category = await _globalCategoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
            {
                return ServiceResponse<bool>.Fail(
                    "Không tìm thấy danh mục.", 
                    HttpStatusCode.NotFound);
            }

            // ✅ Kiểm tra ParentId có tồn tại và không phải chính nó không
            if (dto.ParentId.HasValue && dto.ParentId.Value == id)
            {
                return ServiceResponse<bool>.Fail(
                    "Danh mục cha không thể là chính nó.", 
                    HttpStatusCode.BadRequest);
            }

            // ✅ Kiểm tra ParentId có tồn tại không
            if (dto.ParentId.HasValue)
            {
                var parent = await _globalCategoryRepository.GetByIdAsync(dto.ParentId.Value);
                if (parent == null || parent.IsDeleted)
                {
                    return ServiceResponse<bool>.Fail(
                        "ParentId không hợp lệ hoặc không tồn tại.", 
                        HttpStatusCode.BadRequest);
                }

                // ✅ Fix: Kiểm tra circular reference (A -> B -> A)
                bool hasCircular = await _globalCategoryRepository.HasCircularReferenceAsync(id, dto.ParentId);
                if (hasCircular)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể đặt danh mục cha này vì sẽ tạo vòng lặp tham chiếu (circular reference).", 
                        HttpStatusCode.BadRequest);
                }
            }

            // ✅ New: Kiểm tra duplicate name trong cùng level (trừ chính nó)
            // Chỉ check nếu tên thay đổi hoặc parent thay đổi
            if (category.Name.ToLower() != dto.Name.ToLower() || category.ParentId != dto.ParentId)
            {
                bool nameExists = await _globalCategoryRepository.ExistsByNameInSameLevelAsync(
                    dto.Name, 
                    dto.ParentId, 
                    excludeId: id);
                
                if (nameExists)
                {
                    var levelDescription = dto.ParentId.HasValue 
                        ? "cùng danh mục cha" 
                        : "cấp gốc";
                    return ServiceResponse<bool>.Fail(
                        $"Tên danh mục '{dto.Name}' đã tồn tại ở {levelDescription}. Vui lòng chọn tên khác.", 
                        HttpStatusCode.BadRequest);
                }
            }

            _mapper.Map(dto, category); // Ánh xạ các thay đổi vào entity hiện có
            category.UpdatedAt = DateTime.UtcNow;
            await _globalCategoryRepository.UpdateAsync(category);

            return ServiceResponse<bool>.Success(true, "Cập nhật danh mục thành công.");
        }

        public async Task<ServiceResponse<bool>> DeleteGlobalCategoryAsync(Guid id)
        {
            var category = await _globalCategoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
            {
                return ServiceResponse<bool>.Fail(
                    "Không tìm thấy danh mục.", 
                    HttpStatusCode.NotFound);
            }

            // ✅ Fix: Kiểm tra xem category có products không
            var productCount = await _productRepository.CountByCategoryIdAsync(id);
            if (productCount > 0)
            {
                return ServiceResponse<bool>.Fail(
                    $"Không thể xóa danh mục. Có {productCount} sản phẩm đang sử dụng danh mục này.", 
                    HttpStatusCode.BadRequest);
            }

            // ✅ Fix: Kiểm tra xem category có children không
            var childrenCount = await _globalCategoryRepository.CountChildrenAsync(id);
            if (childrenCount > 0)
            {
                return ServiceResponse<bool>.Fail(
                    $"Không thể xóa danh mục. Có {childrenCount} danh mục con đang sử dụng danh mục này.", 
                    HttpStatusCode.BadRequest);
            }

            // Xóa mềm
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            await _globalCategoryRepository.UpdateAsync(category);

            return ServiceResponse<bool>.Success(true, "Xóa mềm danh mục thành công.");
        }

        public async Task<ServiceResponse<IEnumerable<GetGlobalCategory>>> GetAllGlobalCategoriesAsync(bool includeChildren = false)
        {
            // Tùy chọn: Lấy dạng cây hoặc dạng phẳng.
            IEnumerable<GlobalCategory> categories;
            if (includeChildren)
            {
                // Gọi Repository để lấy dạng cây (nếu đã triển khai)
                categories = await _globalCategoryRepository.GetAllCategoriesWithChildrenAsync();
            }
            else
            {
                // Lấy tất cả (dạng phẳng)
                categories = await _globalCategoryRepository.GetAllAsync();
            }

            var categoryDtos = _mapper.Map<IEnumerable<GetGlobalCategory>>(categories);
            return ServiceResponse<IEnumerable<GetGlobalCategory>>.Success(categoryDtos);
        }

        // ✅ New: Get category by ID
        public async Task<ServiceResponse<GetGlobalCategory>> GetGlobalCategoryByIdAsync(Guid id)
        {
            var category = await _globalCategoryRepository.GetByIdWithChildrenAsync(id);
            if (category == null || category.IsDeleted)
            {
                return ServiceResponse<GetGlobalCategory>.Fail(
                    "Không tìm thấy danh mục.", 
                    HttpStatusCode.NotFound);
            }

            var categoryDto = _mapper.Map<GetGlobalCategory>(category);
            return ServiceResponse<GetGlobalCategory>.Success(categoryDto);
        }

        // ✅ New: Get categories by parent ID
        public async Task<ServiceResponse<IEnumerable<GetGlobalCategory>>> GetCategoriesByParentIdAsync(Guid? parentId)
        {
            IEnumerable<GlobalCategory> categories;
            
            if (parentId.HasValue)
            {
                // Lấy children của parent cụ thể
                var allCategories = await _globalCategoryRepository.GetAllAsync();
                categories = allCategories.Where(c => c.ParentId == parentId.Value);
            }
            else
            {
                // Lấy root categories (ParentId == null)
                var allCategories = await _globalCategoryRepository.GetAllAsync();
                categories = allCategories.Where(c => c.ParentId == null);
            }

            var categoryDtos = _mapper.Map<IEnumerable<GetGlobalCategory>>(categories);
            return ServiceResponse<IEnumerable<GetGlobalCategory>>.Success(categoryDtos);
        }

        // ✅ New: Statistics for Admin dashboard
        public async Task<ServiceResponse<object>> GetStatisticsAsync()
        {
            try
            {
                var totalCategories = await _globalCategoryRepository.GetTotalCountAsync();
                var rootCategories = await _globalCategoryRepository.GetRootCategoriesCountAsync();
                var maxDepth = await _globalCategoryRepository.GetMaxDepthAsync();
                var productCountPerCategory = await _globalCategoryRepository.GetProductCountPerCategoryAsync();
                
                // Count categories without products
                var allCategories = await _globalCategoryRepository.GetAllAsync();
                var categoriesWithoutProducts = allCategories
                    .Where(c => !productCountPerCategory.ContainsKey(c.Id))
                    .Count();

                // Categories with most products (top 5)
                var topCategories = productCountPerCategory
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5)
                    .Select(kvp => new
                    {
                        CategoryId = kvp.Key,
                        CategoryName = allCategories.FirstOrDefault(c => c.Id == kvp.Key)?.Name ?? "Unknown",
                        ProductCount = kvp.Value
                    })
                    .ToList();

                // Average products per category
                var averageProductsPerCategory = productCountPerCategory.Any()
                    ? productCountPerCategory.Values.Average()
                    : 0;

                var statistics = new
                {
                    TotalCategories = totalCategories,
                    RootCategories = rootCategories,
                    MaxDepth = maxDepth,
                    CategoriesWithoutProducts = categoriesWithoutProducts,
                    TotalProducts = productCountPerCategory.Values.Sum(),
                    AverageProductsPerCategory = Math.Round(averageProductsPerCategory, 2),
                    TopCategoriesByProductCount = topCategories
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

