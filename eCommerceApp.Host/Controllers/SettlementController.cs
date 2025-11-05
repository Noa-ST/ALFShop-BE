using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Settlement;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettlementController : ControllerBase
    {
        private readonly ISettlementService _settlementService;

        public SettlementController(ISettlementService settlementService)
        {
            _settlementService = settlementService;
        }

        // ✅ Helper method để lấy UserId từ JWT Claim
        private string GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định người dùng. Vui lòng đăng nhập lại.");
            }
            return userId;
        }

        // ========== Seller Endpoints ==========

        /// <summary>
        /// Lấy số dư của Seller
        /// </summary>
        [HttpGet("my-balance")]
        [Authorize(Roles = "Seller")]
        [ProducesResponseType(typeof(ServiceResponse<SellerBalanceDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetMyBalance()
        {
            try
            {
                var sellerId = GetUserId();
                var response = await _settlementService.GetSellerBalanceAsync(sellerId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo yêu cầu giải ngân
        /// </summary>
        [HttpPost("request")]
        [Authorize(Roles = "Seller")]
        [ProducesResponseType(typeof(ServiceResponse<SettlementDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> CreateSettlementRequest([FromBody] CreateSettlementRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var sellerId = GetUserId();
                var response = await _settlementService.CreateSettlementRequestAsync(sellerId, request);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách settlements của Seller
        /// </summary>
        [HttpGet("my-settlements")]
        [Authorize(Roles = "Seller")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<SettlementDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetMySettlements([FromQuery] SettlementFilterDto? filter = null)
        {
            try
            {
                var sellerId = GetUserId();
                var response = await _settlementService.GetMySettlementsAsync(sellerId, filter);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ========== Admin Endpoints ==========

        /// <summary>
        /// Lấy danh sách pending settlements (cho Admin)
        /// </summary>
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<List<SettlementDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetPendingSettlements()
        {
            try
            {
                var response = await _settlementService.GetPendingSettlementsAsync();
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Approve settlement
        /// </summary>
        [HttpPost("admin/{settlementId}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ApproveSettlement(Guid settlementId)
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest(new { Message = "SettlementId không hợp lệ." });
            }

            try
            {
                var adminId = GetUserId();
                var response = await _settlementService.ApproveSettlementAsync(settlementId, adminId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Process payout (thực hiện giải ngân)
        /// </summary>
        [HttpPost("admin/{settlementId}/process")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ProcessPayout(Guid settlementId, [FromBody] ProcessSettlementRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (settlementId == Guid.Empty)
            {
                return BadRequest(new { Message = "SettlementId không hợp lệ." });
            }

            try
            {
                var adminId = GetUserId();
                var response = await _settlementService.ProcessPayoutAsync(settlementId, adminId, request);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reject settlement
        /// </summary>
        [HttpPost("admin/{settlementId}/reject")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> RejectSettlement(Guid settlementId, [FromBody] string reason)
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest(new { Message = "SettlementId không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new { Message = "Lý do từ chối không được để trống." });
            }

            try
            {
                var adminId = GetUserId();
                var response = await _settlementService.RejectSettlementAsync(settlementId, adminId, reason);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy tất cả settlements với filter (cho Admin)
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<SettlementDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetAllSettlements([FromQuery] SettlementFilterDto? filter = null)
        {
            try
            {
                var response = await _settlementService.GetAllSettlementsAsync(filter);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê settlement (cho Admin)
        /// </summary>
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<object>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var response = await _settlementService.GetSettlementStatisticsAsync(startDate, endDate);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Complete settlement sau khi PayOS confirm transfer thành công
        /// </summary>
        [HttpPost("admin/{settlementId}/complete")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> CompleteSettlement(Guid settlementId)
        {
            if (settlementId == Guid.Empty)
            {
                return BadRequest(new { Message = "SettlementId không hợp lệ." });
            }

            try
            {
                var adminId = GetUserId();
                var response = await _settlementService.CompleteSettlementAsync(settlementId, adminId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tính settlement cho order (auto khi order Delivered) - Internal endpoint
        /// </summary>
        [HttpPost("calculate/{orderId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> CalculateSettlementForOrder(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            try
            {
                var response = await _settlementService.CalculateSettlementForOrderAsync(orderId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}

