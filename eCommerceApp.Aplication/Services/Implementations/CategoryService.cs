using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Category;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class CategoryService(ICategoryRepository categoryRepo, IMapper mapper) : ICategoryService
    {
        public async Task<ServiceResponse> AddAsync(CreateCategory category)
        {
            var mappedData = mapper.Map<Category>(category);
            mappedData.CreatedAt = DateTime.UtcNow;
            mappedData.UpdatedAt = null;
            mappedData.IsDeleted = false;

            int result = await categoryRepo.AddAsync(mappedData);
            return result > 0
                ? new ServiceResponse(true, "Category created successfully.")
                : new ServiceResponse(false, "Failed to create category.");
        }

        public async Task<ServiceResponse> UpdateAsync(UpdateCategory category)
        {
            var existing = await categoryRepo.GetByIdAsync(category.Id);
            if (existing == null || existing.IsDeleted)
                return new ServiceResponse(false, "Category not found.");

            mapper.Map(category, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            int result = await categoryRepo.UpdateAsync(existing);
            return result > 0
                ? new ServiceResponse(true, "Category updated successfully.")
                : new ServiceResponse(false, "Failed to update category.");
        }

        public async Task<ServiceResponse> DeleteAsync(Guid id)
        {
            int result = await categoryRepo.SoftDeleteAsync(id);
            return result > 0
                ? new ServiceResponse(true, "Category deleted (soft delete).")
                : new ServiceResponse(false, "Category not found or failed to delete.");
        }

        public async Task<IEnumerable<GetCategory>> GetAllAsync()
        {
            var data = await categoryRepo.GetAllAsync();
            var active = data.Where(c => !c.IsDeleted);
            return mapper.Map<IEnumerable<GetCategory>>(active);
        }

        public async Task<GetCategory?> GetByIdAsync(Guid id)
        {
            var entity = await categoryRepo.GetByIdWithIncludeAsync(id);
            return entity == null || entity.IsDeleted ? null : mapper.Map<GetCategory>(entity);
        }

        public async Task<IEnumerable<GetCategory>> GetByShopIdAsync(Guid shopId)
        {
            var data = await categoryRepo.GetByShopIdAsync(shopId);
            var active = data.Where(c => !c.IsDeleted);
            return mapper.Map<IEnumerable<GetCategory>>(active);
        }
    }
}
