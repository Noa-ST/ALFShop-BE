using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.ShopCategory;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;

using System.Net;


namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ShopCategoryService : IShopCategoryService
    {
        private readonly IShopCategoryRepository _shopCategoryRepository;
        private readonly IMapper _mapper;

        public ShopCategoryService(IShopCategoryRepository shopCategoryRepository, IMapper mapper)
        {
            _shopCategoryRepository = shopCategoryRepository;
            _mapper = mapper;
        }

        // --- 1. CREATE: Seller tạo danh mục riêng cho Shop ---
        public async Task<ServiceResponse<GetShopCategory>> CreateShopCategoryAsync(Guid shopId, CreateShopCategory dto)
        {
            // ✅ Kiểm tra ParentId có hợp lệ và thuộc Shop hiện tại không
            if (dto.ParentId.HasValue)
            {
                var isOwner = await _shopCategoryRepository.IsShopCategoryOwnerAsync(shopId, dto.ParentId.Value);
                if (!isOwner)
                {
                    return ServiceResponse<GetShopCategory>.Fail(
                        "ParentId không hợp lệ hoặc không thuộc Shop này.", 
                        HttpStatusCode.BadRequest);
                }
            }

            // ✅ New: Kiểm tra duplicate name trong cùng shop và level
            bool nameExists = await _shopCategoryRepository.ExistsByNameInSameShopAndLevelAsync(
                dto.Name, 
                shopId, 
                dto.ParentId);
            
            if (nameExists)
            {
                var levelDescription = dto.ParentId.HasValue 
                    ? "cùng danh mục cha" 
                    : "cấp gốc";
                return ServiceResponse<GetShopCategory>.Fail(
                    $"Tên danh mục '{dto.Name}' đã tồn tại ở {levelDescription} trong shop này. Vui lòng chọn tên khác.", 
                    HttpStatusCode.BadRequest);
            }

            var category = _mapper.Map<ShopCategory>(dto);
            category.ShopId = shopId; // Gán ShopId từ Service Layer (đã được xác thực)
            category.CreatedAt = DateTime.UtcNow;
            category.IsDeleted = false;

            await _shopCategoryRepository.AddAsync(category);

            var categoryDto = _mapper.Map<GetShopCategory>(category);
            return ServiceResponse<GetShopCategory>.Success(categoryDto, "Tạo danh mục Shop thành công.");
        }

        // --- 2. UPDATE: Seller cập nhật danh mục của Shop ---
        public async Task<ServiceResponse<bool>> UpdateShopCategoryAsync(Guid shopId, Guid categoryId, UpdateShopCategory dto)
        {
            if (categoryId != dto.Id)
            {
                return ServiceResponse<bool>.Fail(
                    "ID trong đường dẫn và Body không khớp.", 
                    HttpStatusCode.BadRequest);
            }

            // Lấy category bằng ID
            var category = await _shopCategoryRepository.GetByIdAsync(categoryId);

            // ✅ Kiểm tra tồn tại và quyền sở hữu
            if (category == null || category.ShopId != shopId)
            {
                return ServiceResponse<bool>.Fail(
                    "Không tìm thấy danh mục hoặc không có quyền truy cập.", 
                    HttpStatusCode.NotFound);
            }

            // ✅ Kiểm tra ParentId (Nếu thay đổi)
            if (dto.ParentId.HasValue && dto.ParentId.Value == categoryId)
            {
                return ServiceResponse<bool>.Fail(
                    "Danh mục cha không thể là chính nó.", 
                    HttpStatusCode.BadRequest);
            }

            if (dto.ParentId.HasValue)
            {
                var isOwner = await _shopCategoryRepository.IsShopCategoryOwnerAsync(shopId, dto.ParentId.Value);
                if (!isOwner)
                {
                    return ServiceResponse<bool>.Fail(
                        "ParentId không hợp lệ hoặc không thuộc Shop này.", 
                        HttpStatusCode.BadRequest);
                }

                // ✅ New: Kiểm tra circular reference (A -> B -> A)
                bool hasCircular = await _shopCategoryRepository.HasCircularReferenceAsync(categoryId, dto.ParentId);
                if (hasCircular)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không thể đặt danh mục cha này vì sẽ tạo vòng lặp tham chiếu (circular reference).", 
                        HttpStatusCode.BadRequest);
                }
            }

            // ✅ New: Kiểm tra duplicate name trong cùng shop và level (trừ chính nó)
            // Chỉ check nếu tên thay đổi hoặc parent thay đổi
            if (category.Name.ToLower() != dto.Name.ToLower() || category.ParentId != dto.ParentId)
            {
                bool nameExists = await _shopCategoryRepository.ExistsByNameInSameShopAndLevelAsync(
                    dto.Name, 
                    shopId, 
                    dto.ParentId, 
                    excludeId: categoryId);
                
                if (nameExists)
                {
                    var levelDescription = dto.ParentId.HasValue 
                        ? "cùng danh mục cha" 
                        : "cấp gốc";
                    return ServiceResponse<bool>.Fail(
                        $"Tên danh mục '{dto.Name}' đã tồn tại ở {levelDescription} trong shop này. Vui lòng chọn tên khác.", 
                        HttpStatusCode.BadRequest);
                }
            }

            _mapper.Map(dto, category);
            category.UpdatedAt = DateTime.UtcNow;
            await _shopCategoryRepository.UpdateAsync(category);

            return ServiceResponse<bool>.Success(true, "Cập nhật danh mục Shop thành công.");
        }

        // --- 3. DELETE: Seller xóa mềm danh mục của Shop ---
        public async Task<ServiceResponse<bool>> DeleteShopCategoryAsync(Guid shopId, Guid categoryId)
        {
            var category = await _shopCategoryRepository.GetByIdAsync(categoryId);

            // ✅ Kiểm tra tồn tại và quyền sở hữu
            if (category == null || category.ShopId != shopId)
            {
                return ServiceResponse<bool>.Fail(
                    "Không tìm thấy danh mục hoặc không có quyền truy cập.", 
                    HttpStatusCode.NotFound);
            }

            // ✅ New: Kiểm tra xem category có children không
            var childrenCount = await _shopCategoryRepository.CountChildrenAsync(categoryId);
            if (childrenCount > 0)
            {
                return ServiceResponse<bool>.Fail(
                    $"Không thể xóa danh mục. Có {childrenCount} danh mục con đang sử dụng danh mục này.", 
                    HttpStatusCode.BadRequest);
            }

            // Xóa mềm
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            await _shopCategoryRepository.UpdateAsync(category);

            return ServiceResponse<bool>.Success(true, "Xóa mềm danh mục Shop thành công.");
        }

        // --- 4. READ: Lấy danh sách danh mục của Shop ---
        public async Task<ServiceResponse<IEnumerable<GetShopCategory>>> GetShopCategoriesByShopIdAsync(Guid shopId)
        {
            var categories = await _shopCategoryRepository.GetByShopIdAsync(shopId, includeChildren: true);

            var dtos = _mapper.Map<IEnumerable<GetShopCategory>>(categories);
            return ServiceResponse<IEnumerable<GetShopCategory>>.Success(dtos);
        }

        // --- 5. READ: Lấy chi tiết danh mục ---
        public async Task<ServiceResponse<GetShopCategory>> GetShopCategoryDetailAsync(Guid shopId, Guid categoryId)
        {
            var category = await _shopCategoryRepository.GetByIdWithChildrenAsync(categoryId);

            // ✅ Kiểm tra tồn tại và quyền sở hữu
            if (category == null || category.ShopId != shopId)
            {
                return ServiceResponse<GetShopCategory>.Fail(
                    "Không tìm thấy danh mục hoặc không có quyền truy cập.", 
                    HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<GetShopCategory>(category);
            return ServiceResponse<GetShopCategory>.Success(dto);
        }
    }
}