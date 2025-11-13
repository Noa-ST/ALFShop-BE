using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Review;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // Lấy danh sách review theo sản phẩm (có phân trang)
        // Route tuyệt đối để khớp cấu trúc: api/products/{productId}/reviews
        [HttpGet("/api/products/{productId:guid}/reviews")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<GetReview>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool onlyApproved = true)
        {
            var result = await _reviewService.GetByProductAsync(productId, page, pageSize, onlyApproved);
            return Ok(result);
        }

        // Lấy review của chính người dùng cho một sản phẩm
        // Giúp FE kiểm soát form: biết đã từng đánh giá hay chưa
        [HttpGet("/api/products/{productId:guid}/reviews/me")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyReview(Guid productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var review = await _reviewService.GetMyReviewForProductAsync(productId, userId);
            return Ok(new { hasReviewed = review != null, review });
        }

        // Admin: lấy danh sách review chưa duyệt
        [HttpGet("/api/Admin/reviews/pending")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<GetReview>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _reviewService.GetPendingAsync(page, pageSize);
            return Ok(result);
        }

        // Tạo review mới (yêu cầu đăng nhập)
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateReview dto)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"[Review Create] ModelState Invalid. Errors:");
                foreach (var entry in ModelState.Values)
                {
                    foreach (var error in entry.Errors)
                    {
                        Console.WriteLine($"  - {error.ErrorMessage}");
                    }
                }
                Console.WriteLine($"[Review Create] DTO received: ProductId={dto?.ProductId}, Rating={dto?.Rating}, Comment={dto?.Comment}");
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var response = await _reviewService.CreateAsync(dto, userId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Cập nhật review của chính người dùng
        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReview dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var response = await _reviewService.UpdateAsync(id, dto, userId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Xóa mềm review của chính người dùng
        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var response = await _reviewService.DeleteAsync(id, userId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Admin: duyệt review
        [HttpPut("/api/Admin/reviews/{id:guid}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Approve(Guid id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var response = await _reviewService.ApproveAsync(id, adminId);
            return StatusCode((int)response.StatusCode, response);
        }

        // Admin: từ chối review
        [HttpPut("/api/Admin/reviews/{id:guid}/reject")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Reject(Guid id, [FromQuery] string reason = "")
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var response = await _reviewService.RejectAsync(id, adminId, reason);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}