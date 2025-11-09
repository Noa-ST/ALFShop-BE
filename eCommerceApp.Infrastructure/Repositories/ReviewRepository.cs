using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eCommerceApp.Infrastructure.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        private readonly AppDbContext _context;
        public ReviewRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Review?> GetByIdAsync(Guid id)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        public async Task<Review?> GetUserReviewForProductAsync(Guid productId, string userId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId && !r.IsDeleted);
        }

        public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetByProductIdAsync(Guid productId, int page, int pageSize, bool onlyApproved)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && !r.IsDeleted);

            if (onlyApproved)
                query = query.Where(r => r.Status == ReviewStatus.Approved);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<int> ApproveAsync(Guid id, string adminId)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (review == null) return 0;
            review.Status = ReviewStatus.Approved;
            review.UpdatedAt = DateTime.UtcNow;
            _context.Reviews.Update(review);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> RejectAsync(Guid id, string adminId, string reason)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (review == null) return 0;
            review.Status = ReviewStatus.Rejected;
            review.RejectionReason = reason;
            review.UpdatedAt = DateTime.UtcNow;
            _context.Reviews.Update(review);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SoftDeleteAsync(Guid id, string userId)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && !r.IsDeleted);
            if (review == null) return 0;
            review.IsDeleted = true;
            review.UpdatedAt = DateTime.UtcNow;
            _context.Reviews.Update(review);
            return await _context.SaveChangesAsync();
        }
    }
}