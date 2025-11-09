using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Domain.Interfaces
{
    public interface IReviewRepository : IGeneric<Review>
    {
        // ❌ Bỏ GetByIdAsync vì đã có trong IGeneric<Review> để tránh CS0108
        Task<Review?> GetUserReviewForProductAsync(Guid productId, string userId);
        Task<(IEnumerable<Review> Reviews, int TotalCount)> GetByProductIdAsync(Guid productId, int page, int pageSize, bool onlyApproved);
        Task<int> ApproveAsync(Guid id, string adminId);
        Task<int> RejectAsync(Guid id, string adminId, string reason);
        Task<int> SoftDeleteAsync(Guid id, string userId);
    }
}