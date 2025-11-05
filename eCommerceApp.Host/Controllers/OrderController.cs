using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using eCommerceApp.Aplication.DTOs.Order;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.DTOs;
using System.Security.Claims;
using System.Net;

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
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

        [HttpPost("create")]
        [Authorize] // Yêu cầu đăng nhập
        [ProducesResponseType(typeof(ServiceResponse<List<OrderResponseDTO>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] OrderCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // ✅ Fix: Luôn lấy CustomerId từ JWT, không cho phép override từ DTO
                var userId = GetUserId();
                dto.CustomerId = userId; // Override để đảm bảo security

                var response = await _orderService.CreateOrderAsync(dto);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("myOrders")]
        [Authorize] // Yêu cầu đăng nhập
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<OrderResponseDTO>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MyOrders([FromQuery] OrderFilterDto? filter = null)
        {
            try
            {
                // ✅ Fix: Không cho phép truyền customerId từ query - security issue
                var userId = GetUserId();
                var response = await _orderService.GetMyOrdersAsync(userId, filter);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("shopOrders")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Add authorization
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<OrderResponseDTO>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ShopOrders([FromQuery] Guid shopId, [FromQuery] OrderFilterDto? filter = null)
        {
            if (shopId == Guid.Empty)
            {
                return BadRequest(new { Message = "ShopId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _orderService.GetShopOrdersAsync(shopId, userId, filter);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")] // ✅ Fix: Admin only
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<OrderResponseDTO>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> All([FromQuery] OrderFilterDto? filter = null)
        {
            try
            {
                var response = await _orderService.GetAllOrdersAsync(filter);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Search and Filter with Pagination
        [HttpGet("search")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<OrderResponseDTO>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> SearchAndFilter([FromQuery] OrderFilterDto filter)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var response = await _orderService.SearchAndFilterAsync(filter, userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Statistics Endpoint for Admin
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<object>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var response = await _orderService.GetStatisticsAsync(startDate, endDate);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Cancel Order Endpoint
        [HttpPost("{id:guid}/cancel")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _orderService.CancelOrderAsync(id, userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Update Tracking Number
        [HttpPut("{id:guid}/tracking-number")]
        [Authorize(Roles = "Seller,Admin")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> UpdateTrackingNumber(Guid id, [FromBody] UpdateTrackingNumberDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
            {
                return BadRequest(new { Message = "Mã vận chuyển không được để trống." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _orderService.UpdateTrackingNumberAsync(id, dto.TrackingNumber, userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPut("updateStatus/{id}")]
        [Authorize] // Yêu cầu đăng nhập
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] OrderUpdateStatusDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _orderService.UpdateStatusAsync(id, dto, userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: GET /api/Order/{id}
        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<OrderResponseDTO>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _orderService.GetOrderByIdAsync(id, userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Customer Confirm Delivery
        [HttpPost("{id:guid}/confirm-delivery")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ConfirmDelivery(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _orderService.ConfirmDeliveryAsync(id, userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
