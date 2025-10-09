using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class ProductImageRepository : GenericRepository<ProductImage>, IProductImageRepository
    {
        private readonly AppDbContext _context;

        public ProductImageRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(Guid productId)
        {
            return await _context.ProductImages
                .Where(pi => pi.ProductId == productId && !pi.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> SoftDeleteByProductIdAsync(Guid productId)
        {
            var images = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync();

            foreach (var img in images)
            {
                img.IsDeleted = true;
                img.UpdatedAt = DateTime.UtcNow;
            }

            _context.ProductImages.UpdateRange(images);
            return await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ProductImage> images)
        {
            await _context.ProductImages.AddRangeAsync(images);
            await _context.SaveChangesAsync();
        }
    }
}
