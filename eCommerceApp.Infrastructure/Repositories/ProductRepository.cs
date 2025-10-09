using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Load toàn bộ Product kèm Shop, Category, Images (dành cho trang chủ hoặc admin)
        public new async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // ✅ Lấy theo Shop (Seller quản lý sản phẩm của mình)
        public async Task<IEnumerable<Product>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.ShopId == shopId && !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // ✅ Lấy theo Category (hiển thị danh mục)
        public async Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId)
        {
            return await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Images)
                .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // ✅ Lấy chi tiết 1 sản phẩm (trang detail)
        public async Task<Product?> GetDetailByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        // ✅ Soft Delete (đánh dấu xoá)
        public async Task<int> SoftDeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return 0;

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Update(product);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> AddWithImagesAsync(Product product, IEnumerable<ProductImage>? images)
        {
            // Dùng ExecutionStrategy để hỗ trợ retry (chống lỗi InvalidOperationException)
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.Products.AddAsync(product);

                    if (images != null && images.Any())
                        await _context.ProductImages.AddRangeAsync(images);

                    int result = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

    }
}
