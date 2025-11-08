using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        // ✅ Load toàn bộ Product kèm Shop, GlobalCategory, Images (dành cho trang chủ hoặc admin)
        public new async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Shop)
                // ✅ SỬA: Category cũ -> GlobalCategory mới
                .Include(p => p.GlobalCategory)
                .Include(p => p.Images) // Đổi từ Images sang ProductImages (theo Entity Product.cs)
                .Where(p => !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // ✅ Lấy theo Shop (Seller quản lý sản phẩm của mình)
        public async Task<IEnumerable<Product>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Products
                // ✅ SỬA: Category cũ -> GlobalCategory mới
                .Include(p => p.GlobalCategory)
                .Include(p => p.Images) // Đổi từ Images sang ProductImages
                .Where(p => p.ShopId == shopId && !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // ✅ Lấy theo Global Category (hiển thị danh mục công khai)
        public async Task<IEnumerable<Product>> GetByGlobalCategoryIdAsync(Guid globalCategoryId) // ✅ SỬA TÊN PHƯƠNG THỨC
        {
            return await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Images) // Đổi từ Images sang ProductImages
                                               // ✅ SỬA: p.CategoryId cũ -> p.GlobalCategoryId mới
                .Where(p => p.GlobalCategoryId == globalCategoryId && !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // ✅ Lấy chi tiết 1 sản phẩm (trang detail)
        public async Task<Product?> GetDetailByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Shop)
                // ✅ SỬA: Category cũ -> GlobalCategory mới
                .Include(p => p.GlobalCategory)
                .Include(p => p.Images) // Đổi từ Images sang ProductImages
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        // ✅ Soft Delete (đánh dấu xoá) - Giữ nguyên
        public async Task<int> SoftDeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return 0;

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Update(product);
            return await _context.SaveChangesAsync();
        }

        // ✅ New: Update product status and save changes (for approve/reject)
        public async Task<int> UpdateStatusAsync(Product product)
        {
            _context.Products.Update(product);
            return await _context.SaveChangesAsync();
        }

        // ✅ AddWithImagesAsync - Sửa để gán ProductId cho images và tránh duplicate Id
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
                    // Save để lấy Product.Id
                    await _context.SaveChangesAsync();

                    // ✅ Tạo lại list images mới để tránh tracking conflicts và đảm bảo Id duy nhất
                    if (images != null && images.Any())
                    {
                        var imagesToAdd = new List<ProductImage>();
                        var usedIds = new HashSet<Guid>();
                        
                        foreach (var image in images)
                        {
                            // Đảm bảo Id duy nhất
                            Guid imageId;
                            if (image.Id == Guid.Empty || usedIds.Contains(image.Id))
                            {
                                do
                                {
                                    imageId = Guid.NewGuid();
                                } while (usedIds.Contains(imageId));
                            }
                            else
                            {
                                imageId = image.Id;
                            }
                            
                            usedIds.Add(imageId);
                            
                            // Tạo instance mới để tránh tracking conflicts
                            imagesToAdd.Add(new ProductImage
                            {
                                Id = imageId,
                                ProductId = product.Id,
                                Url = image.Url,
                                CreatedAt = image.CreatedAt,
                                UpdatedAt = image.UpdatedAt,
                                IsDeleted = image.IsDeleted
                            });
                        }
                        
                        await _context.ProductImages.AddRangeAsync(imagesToAdd);
                    }

                    int result = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        // ✅ New: Search and filter with pagination
        public async Task<(IEnumerable<Product> Products, int TotalCount)> SearchAndFilterAsync(
            string? keyword,
            Guid? shopId,
            Guid? categoryId,
            Domain.Enums.ProductStatus? status,
            decimal? minPrice,
            decimal? maxPrice,
            string sortBy,
            string sortOrder,
            int page,
            int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Shop)
                .Include(p => p.GlobalCategory)
                .Include(p => p.Images)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(p => 
                    p.Name.ToLower().Contains(keyword) || 
                    (p.Description != null && p.Description.ToLower().Contains(keyword)));
            }

            if (shopId.HasValue)
                query = query.Where(p => p.ShopId == shopId.Value);

            if (categoryId.HasValue)
                query = query.Where(p => p.GlobalCategoryId == categoryId.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Get total count before pagination
            int totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "price" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(p => p.Price) 
                    : query.OrderByDescending(p => p.Price),
                "name" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(p => p.Name) 
                    : query.OrderByDescending(p => p.Name),
                "rating" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(p => p.AverageRating) 
                    : query.OrderByDescending(p => p.AverageRating),
                "updatedat" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(p => p.UpdatedAt ?? p.CreatedAt) 
                    : query.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt),
                _ => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(p => p.CreatedAt) 
                    : query.OrderByDescending(p => p.CreatedAt) // Default: createdAt
            };

            // Apply pagination
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (products, totalCount);
        }

        // ✅ New: Stock management
        public async Task<int> UpdateStockQuantityAsync(Guid productId, int quantityChange)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.IsDeleted)
                return 0;

            product.StockQuantity += quantityChange;
            if (product.StockQuantity < 0)
                product.StockQuantity = 0; // Prevent negative stock

            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            return await _context.SaveChangesAsync();
        }

        public async Task<Product?> GetByIdForUpdateAsync(Guid id)
        {
            // Get with tracking for update operations
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        // ✅ New: Count products by category
        public async Task<int> CountByCategoryIdAsync(Guid categoryId)
        {
            return await _context.Products
                .CountAsync(p => p.GlobalCategoryId == categoryId && !p.IsDeleted);
        }
        
        // ✅ New: Update product with images in transaction
        public async Task<int> UpdateWithImagesAsync(Product product, IEnumerable<ProductImage>? newImages)
        {
            // ✅ Fix: Dùng ExecutionStrategy để hỗ trợ retry (giống như AddWithImagesAsync)
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Products.Update(product);
                    
                    // Update images if provided
                    if (newImages != null && newImages.Any())
                    {
                        // Soft delete old images
                        var existingImages = await _context.ProductImages
                            .Where(pi => pi.ProductId == product.Id && !pi.IsDeleted)
                            .ToListAsync();
                        
                        foreach (var oldImage in existingImages)
                        {
                            oldImage.IsDeleted = true;
                            oldImage.UpdatedAt = DateTime.UtcNow;
                        }
                        
                        if (existingImages.Any())
                        {
                            _context.ProductImages.UpdateRange(existingImages);
                        }
                        
                        // Add new images
                        await _context.ProductImages.AddRangeAsync(newImages);
                    }
                    
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
        
        // ✅ New: Recalculate product rating from reviews
        public async Task<int> RecalculateRatingAsync(Guid productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.IsDeleted)
                return 0;
            
            // Get all approved reviews for this product
            var approvedReviews = await _context.Reviews
                .Where(r => r.ProductId == productId && r.Status == Domain.Enums.ReviewStatus.Approved && !r.IsDeleted)
                .ToListAsync();
            
            if (approvedReviews.Count == 0)
            {
                product.AverageRating = 0.0f;
                product.ReviewCount = 0;
            }
            else
            {
                float totalRating = approvedReviews.Sum(r => r.Rating);
                product.AverageRating = totalRating / approvedReviews.Count;
                product.ReviewCount = approvedReviews.Count;
            }
            
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            return await _context.SaveChangesAsync();
        }
        
        // ✅ New: Get product statistics
        public async Task<ProductStatistics> GetProductStatisticsAsync()
        {
            var totalProducts = await _context.Products.CountAsync(p => !p.IsDeleted);
            var pendingProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.Status == Domain.Enums.ProductStatus.Pending);
            var approvedProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.Status == Domain.Enums.ProductStatus.Approved);
            var rejectedProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.Status == Domain.Enums.ProductStatus.Rejected);
            var outOfStockProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.StockQuantity == 0);
            var lowStockProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.StockQuantity > 0 && p.StockQuantity <= 10);
            
            var totalRevenue = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order != null && oi.Order.Status == Domain.Enums.OrderStatus.Delivered && !oi.Order.IsDeleted)
                .SumAsync(oi => (decimal?)oi.Quantity * oi.PriceAtPurchase) ?? 0;
            
            return new ProductStatistics
            {
                TotalProducts = totalProducts,
                PendingProducts = pendingProducts,
                ApprovedProducts = approvedProducts,
                RejectedProducts = rejectedProducts,
                OutOfStockProducts = outOfStockProducts,
                LowStockProducts = lowStockProducts,
                TotalRevenue = totalRevenue
            };
        }
    }
}