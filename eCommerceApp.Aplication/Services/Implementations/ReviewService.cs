using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Review;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using System.Net;
using System.Linq; // ✅ cần cho ToList()

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
                return ServiceResponse.Fail("Rating phải từ 1 đến 5.", HttpStatusCode.BadRequest);

            var existing = await _uow.Reviews.GetUserReviewForProductAsync(dto.ProductId, userId);
            if (existing != null)
                return ServiceResponse.Fail("Bạn đã đánh giá sản phẩm này rồi.", HttpStatusCode.BadRequest);

            var product = await _uow.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                return ServiceResponse.Fail("Sản phẩm không tồn tại.", HttpStatusCode.NotFound);

            // ✅ Debug logging
            Console.WriteLine($"[DEBUG Review] userId={userId}, productId={dto.ProductId}");

            // ✅ Verified purchase: chỉ cho phép review nếu đã mua và đơn hàng đã Delivered & Paid
            var userOrders = await _uow.Orders.GetOrdersByCustomerIdAsync(userId);
            var ordersList = userOrders.ToList();
            Console.WriteLine($"[DEBUG Review] userOrders count={ordersList.Count}");
            foreach (var order in ordersList)
            {
                var hasProduct = order.Items.Any(i => i.ProductId == dto.ProductId);
                Console.WriteLine($"[DEBUG Review] order={order.Id}, status={order.Status}, paymentStatus={order.PaymentStatus}, hasProduct={hasProduct}");
            }

            var isVerifiedPurchase = userOrders.Any(o =>
                o.Status == OrderStatus.Delivered &&
                o.PaymentStatus == PaymentStatus.Paid &&
                o.Items.Any(i => i.ProductId == dto.ProductId));

            Console.WriteLine($"[DEBUG Review] isVerifiedPurchase={isVerifiedPurchase}");

            if (!isVerifiedPurchase)
                return ServiceResponse.Fail("Chỉ khách đã mua và nhận hàng mới được đánh giá.", HttpStatusCode.Forbidden);

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
                return ServiceResponse.Fail("Review không tồn tại.", HttpStatusCode.NotFound);

            if (review.UserId != userId)
                return ServiceResponse.Fail("Bạn không có quyền sửa review này.", HttpStatusCode.Forbidden);

            if (dto.Rating < 1 || dto.Rating > 5)
                return ServiceResponse.Fail("Rating phải từ 1 đến 5.", HttpStatusCode.BadRequest);

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
                return ServiceResponse.Fail("Không thể xóa review.", HttpStatusCode.NotFound);

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
                Data = dtos.ToList(), // ✅ đổi từ Items -> Data
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<GetReview?> GetMyReviewForProductAsync(Guid productId, string userId)
        {
            var review = await _uow.Reviews.GetUserReviewForProductAsync(productId, userId);
            if (review == null) return null;
            return _mapper.Map<GetReview>(review);
        }

        public async Task<ServiceResponse> ApproveAsync(Guid id, string adminId)
        {
            var review = await _uow.Reviews.GetByIdAsync(id);
            if (review == null)
                return ServiceResponse.Fail("Review không tồn tại.", HttpStatusCode.NotFound);

            var changed = await _uow.Reviews.ApproveAsync(id, adminId);
            if (changed == 0)
                return ServiceResponse.Fail("Duyệt review thất bại.", HttpStatusCode.BadRequest);

            // Recalculate product & shop ratings
            await _productService.RecalculateRatingAsync(review.ProductId);
            var product = await _uow.Products.GetByIdAsync(review.ProductId);
            if (product != null)
            {
                await _shopService.RecalculateRatingAsync(product.ShopId);
            }

            return ServiceResponse.Success("Đã duyệt review.");
        }

        public async Task<ServiceResponse> RejectAsync(Guid id, string adminId, string reason)
        {
            var changed = await _uow.Reviews.RejectAsync(id, adminId, reason);
            if (changed == 0)
                return ServiceResponse.Fail("Từ chối review thất bại.", HttpStatusCode.BadRequest);

            return ServiceResponse.Success("Đã từ chối review với lý do.");
        }
    }
}