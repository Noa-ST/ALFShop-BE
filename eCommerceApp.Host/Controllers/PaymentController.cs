using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net;

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // ✅ Helper method để lấy UserId từ JWT Claim với validation
        private string GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định người dùng. Vui lòng đăng nhập lại.");
            }
            return userId;
        }

        [HttpPost("{orderId}/process")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<PayOSCreatePaymentResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Process(Guid orderId, [FromBody] ProcessPaymentRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (orderId == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _paymentService.ProcessPaymentAsync(orderId, dto.Method, userId);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPut("{paymentId}/status")]
        [Authorize(Roles = "Admin")] // ✅ Fix: Chỉ Admin mới có thể update payment status thủ công
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid paymentId, [FromBody] UpdatePaymentStatusRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (paymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "PaymentId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _paymentService.UpdatePaymentStatusAsync(
                    paymentId, 
                    dto.Status, 
                    userId,
                    changedBy: "Admin", 
                    reason: dto.Reason);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("refund")]
        [Authorize(Roles = "Admin,Seller")] // ✅ Fix: Admin và Seller có thể refund
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Refund([FromBody] RefundRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var result = await _paymentService.RefundAsync(request, userId);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("{paymentId}/history")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<List<PaymentHistory>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetHistory(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "PaymentId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _paymentService.GetPaymentHistoryAsync(paymentId, userId);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Get Payment by OrderId
        [HttpGet("order/{orderId}")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<Payment>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPaymentByOrderId(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _paymentService.GetPaymentByOrderIdAsync(orderId, userId);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Cancel Payment Link
        [HttpPost("{paymentId}/cancel")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CancelPaymentLink(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "PaymentId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _paymentService.CancelPaymentLinkAsync(paymentId, userId);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Retry Failed Payment
        [HttpPost("{paymentId}/retry")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<PayOSCreatePaymentResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> RetryPayment(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "PaymentId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var result = await _paymentService.RetryPaymentAsync(paymentId, userId);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Payment Statistics (Admin only)
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<object>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _paymentService.GetPaymentStatisticsAsync(startDate, endDate);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Expire Payment Links (Background job - Admin only)
        [HttpPost("admin/expire-links")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<int>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ExpirePaymentLinks()
        {
            try
            {
                var result = await _paymentService.ExpirePaymentLinksAsync();
                return StatusCode((int)result.StatusCode, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
