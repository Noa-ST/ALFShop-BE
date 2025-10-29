// File: eCommerceApp.Aplication/Services/Implementations/GlobalCategoryService.cs

using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.GlobalCategory;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class GlobalCategoryService : IGlobalCategoryService
    {
        private readonly IGlobalCategoryRepository _globalCategoryRepository;
        private readonly IMapper _mapper;

        public GlobalCategoryService(IGlobalCategoryRepository globalCategoryRepository, IMapper mapper)
        {
            _globalCategoryRepository = globalCategoryRepository;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<GetGlobalCategory>> CreateGlobalCategoryAsync(CreateGlobalCategory dto)
        {
            // Kiểm tra ParentId có tồn tại không (nếu được cung cấp)
            if (dto.ParentId.HasValue)
            {
                var parent = await _globalCategoryRepository.GetByIdAsync(dto.ParentId.Value);
                if (parent == null || parent.IsDeleted)
                {
                    return ServiceResponse<GetGlobalCategory>.Fail("ParentId không hợp lệ hoặc không tồn tại.");
                }
            }

            var category = _mapper.Map<GlobalCategory>(dto);
            // AuditableEntity sẽ tự động set CreatedAt
            await _globalCategoryRepository.AddAsync(category);

            var categoryDto = _mapper.Map<GetGlobalCategory>(category);
            return ServiceResponse<GetGlobalCategory>.Success(categoryDto, "Tạo danh mục toàn cầu thành công.");
        }


        public async Task<ServiceResponse<bool>> UpdateGlobalCategoryAsync(Guid id, UpdateGlobalCategory dto)
        {
            var category = await _globalCategoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
            {
                return ServiceResponse<bool>.Fail("Không tìm thấy danh mục.");
            }

            // Kiểm tra ParentId có tồn tại và không phải chính nó không
            if (dto.ParentId.HasValue && dto.ParentId.Value == id)
            {
                return ServiceResponse<bool>.Fail("Danh mục cha không thể là chính nó.");
            }
            if (dto.ParentId.HasValue)
            {
                var parent = await _globalCategoryRepository.GetByIdAsync(dto.ParentId.Value);
                if (parent == null || parent.IsDeleted)
                {
                    return ServiceResponse<bool>.Fail("ParentId không hợp lệ hoặc không tồn tại.");
                }
            }

            _mapper.Map(dto, category); // Ánh xạ các thay đổi vào entity hiện có
            await _globalCategoryRepository.UpdateAsync(category);

            return ServiceResponse<bool>.Success(true, "Cập nhật danh mục thành công.");
        }

        public async Task<ServiceResponse<bool>> DeleteGlobalCategoryAsync(Guid id)
        {
            var category = await _globalCategoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
            {
                return ServiceResponse<bool>.Fail("Không tìm thấy danh mục.");
            }

            // Xóa mềm
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow; // Cập nhật thời gian xóa
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
    }
}