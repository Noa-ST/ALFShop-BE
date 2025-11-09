using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Review;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IProductService _productService;
        private readonly IShopService _shopService;

        public ReviewService(IUnitOfWork uow, IMapper mapper, IProductService productService, IShopService shopService)
        {
            _uow = uow;
            _mapper = mapper;
            _productService = productService;
            _shopService = shopService;
        }

        public async Task<ServiceResponse> CreateAsync(CreateReview dto, string userId)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                return ServiceResponse.Error("Rating phải từ 1 đến 5.", System.Net.HttpStatusCode.BadRequest);

            var existing = await _uow.Reviews.GetUserReviewForProductAsync(dto.ProductId, userId);
            if (existing != null)
                return ServiceResponse.Error("Bạn đã đánh giá sản phẩm này rồi.", System.Net.HttpStatusCode.BadRequest);

            var product = await _uow.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                return ServiceResponse.Error("Sản phẩm không tồn tại.", System.Net.HttpStatusCode.NotFound);

            var review = new Review
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                Status = ReviewStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _uow.Reviews.AddAsync(review);
            await _uow.SaveChangesAsync();

            return ServiceResponse.Success("Gửi review thành công, chờ duyệt.");
        }

        public async Task<ServiceResponse> UpdateAsync(Guid id, UpdateReview dto, string userId)
        {
            var review = await _uow.Reviews.GetByIdAsync(id);
            if (review == null)
                return ServiceResponse.Error("Review không tồn tại.", System.Net.HttpStatusCode.NotFound);

            if (review.UserId != userId)
                return ServiceResponse.Error("Bạn không có quyền sửa review này.", System.Net.HttpStatusCode.Forbidden);

            if (dto.Rating < 1 || dto.Rating > 5)
                return ServiceResponse.Error("Rating phải từ 1 đến 5.", System.Net.HttpStatusCode.BadRequest);

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;
            review.Status = ReviewStatus.Pending; // sửa sẽ lại về Pending
            review.UpdatedAt = DateTime.UtcNow;

            await _uow.Reviews.UpdateAsync(review);
            await _uow.SaveChangesAsync();

            return ServiceResponse.Success("Cập nhật review thành công, chờ duyệt.");
        }

        public async Task<ServiceResponse> DeleteAsync(Guid id, string userId)
        {
            var affected = await _uow.Reviews.SoftDeleteAsync(id, userId);
            if (affected == 0)
                return ServiceResponse.Error("Không thể xóa review.", System.Net.HttpStatusCode.NotFound);

            return ServiceResponse.Success("Đã xóa review.");
        }

        public async Task<PagedResult<GetReview>> GetByProductAsync(Guid productId, int page = 1, int pageSize = 20, bool onlyApproved = true)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var (items, total) = await _uow.Reviews.GetByProductIdAsync(productId, page, pageSize, onlyApproved);
            var dtos = _mapper.Map<IEnumerable<GetReview>>(items);

            return new PagedResult<GetReview>
            {
                Items = dtos.ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ServiceResponse> ApproveAsync(Guid id, string adminId)
        {
            var review = await _uow.Reviews.GetByIdAsync(id);
            if (review == null)
                return ServiceResponse.Error("Review không tồn tại.", System.Net.HttpStatusCode.NotFound);

            var changed = await _uow.Reviews.ApproveAsync(id, adminId);
            if (changed == 0)
                return ServiceResponse.Error("Duyệt review thất bại.", System.Net.HttpStatusCode.BadRequest);

            // Recalculate product & shop ratings
            await _productService.RecalculateRatingAsync(review.ProductId);
            var product = await _uow.Products.GetByIdAsync(review.ProductId);
            if (product != null)
            {
                await _shopService.RecalculateRatingAsync(product.ShopId);
            }

            return ServiceResponse.Success("Đã duyệt review.");
        }

        public async Task<ServiceResponse> RejectAsync(Guid id, string adminId)
        {
            var changed = await _uow.Reviews.RejectAsync(id, adminId);
            if (changed == 0)
                return ServiceResponse.Error("Từ chối review thất bại.", System.Net.HttpStatusCode.BadRequest);

            return ServiceResponse.Success("Đã từ chối review.");
        }
    }
}