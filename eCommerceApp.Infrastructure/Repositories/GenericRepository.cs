using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    /// <summary>
    /// Generic Repository dùng cho các entity (TEntity).
    /// Cung cấp CRUD cơ bản để tái sử dụng, tránh lặp code.
    /// </summary>
    /// <typeparam name="TEntity">Entity mà repository thao tác</typeparam>
    public abstract class GenericRepository<TEntity>(AppDbContext context) : IGeneric<TEntity> where TEntity : AuditableEntity

    {
        /// <summary>
        /// Thêm một entity mới vào database.
        /// </summary>
        public async Task<int> AddAsync(TEntity entity)
        {
            context.Set<TEntity>().Add(entity);
            return await context.SaveChangesAsync();
        }

        /// <summary>
        /// Xóa entity theo Id (Guid).
        /// Trả về 0 nếu không tìm thấy entity.
        /// </summary>
        public async Task<int> DeleteAsync(Guid id)
        {
            var entity = await context.Set<TEntity>().FindAsync(id);
            if (entity is null)
                return 0;

            context.Set<TEntity>().Remove(entity);
            return await context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy một entity theo Id.
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id) 
        {
            var result = await context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
            return result; 
        }

        /// <summary>
        /// Lấy tất cả entity. (Nếu TEntity là Auditable, cần thêm !IsDeleted)
        /// </summary>
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await context.Set<TEntity>().AsNoTracking().ToListAsync();
            // Lưu ý: Global Query Filter (HasQueryFilter) trong AppDbContext sẽ tự động xử lý !IsDeleted.
        }

        /// <summary>
        /// Cập nhật entity đã có.
        /// </summary>
        public async Task<int> UpdateAsync(TEntity entity)
        {
            context.Set<TEntity>().Update(entity);
            return await context.SaveChangesAsync();
        }
    }
}
