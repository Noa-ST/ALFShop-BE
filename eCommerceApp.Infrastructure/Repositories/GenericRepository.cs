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
        /// Thêm một entity mới vào context (không tự động save).
        /// Return 1 để indicate entity đã được add vào context.
        /// </summary>
        public async Task<int> AddAsync(TEntity entity)
        {
            await context.Set<TEntity>().AddAsync(entity);
            // ✅ Không tự động save - để UnitOfWork quản lý
            return 1;
        }

        /// <summary>
        /// Xóa entity theo Id (Guid) từ context (không tự động save).
        /// Trả về 0 nếu không tìm thấy entity, 1 nếu đã mark để delete.
        /// </summary>
        public async Task<int> DeleteAsync(Guid id)
        {
            var entity = await context.Set<TEntity>().FindAsync(id);
            if (entity is null)
                return 0;

            context.Set<TEntity>().Remove(entity);
            // ✅ Không tự động save - để UnitOfWork quản lý
            return 1;
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
        /// Cập nhật entity đã có trong context (không tự động save).
        /// Return 1 để indicate entity đã được update trong context.
        /// </summary>
        public async Task<int> UpdateAsync(TEntity entity)
        {
            context.Set<TEntity>().Update(entity);
            // ✅ Không tự động save - để UnitOfWork quản lý
            await Task.CompletedTask; // Để giữ async signature
            return 1;
        }
    }
}
