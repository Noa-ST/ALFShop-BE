using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Review;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IReviewService
    {
        Task<ServiceResponse> CreateAsync(CreateReview dto, string userId);
        Task<ServiceResponse> UpdateAsync(Guid id, UpdateReview dto, string userId);
        Task<ServiceResponse> DeleteAsync(Guid id, string userId);
        Task<PagedResult<GetReview>> GetByProductAsync(Guid productId, int page = 1, int pageSize = 20, bool onlyApproved = true);
        Task<GetReview?> GetMyReviewForProductAsync(Guid productId, string userId);
        Task<PagedResult<GetReview>> GetPendingAsync(int page = 1, int pageSize = 20);
        Task<ServiceResponse> ApproveAsync(Guid id, string adminId);
        Task<ServiceResponse> RejectAsync(Guid id, string adminId, string reason);
    }
}