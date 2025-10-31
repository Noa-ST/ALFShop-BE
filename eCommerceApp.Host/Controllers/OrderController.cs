using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using eCommerceApp.Application.DTOs.Order;
using eCommerceApp.Application.Services.Interfaces;
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

        // Helper method để lấy UserId từ JWT Claim
        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        [HttpPost("create")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> Create([FromBody] OrderCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Set CustomerId từ JWT nếu chưa có trong DTO
            if (string.IsNullOrEmpty(dto.CustomerId))
            {
                dto.CustomerId = GetUserId();
            }

            // Validate CustomerId
            if (string.IsNullOrEmpty(dto.CustomerId))
            {
                return BadRequest(new { Message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
            }

            var response = await _orderService.CreateOrderAsync(dto);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("myOrders")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> MyOrders([FromQuery] string? customerId = null)
        {
            // Nếu không có customerId trong query, lấy từ JWT
            var userId = customerId ?? GetUserId();
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { Message = "Không thể xác định người dùng." });
            }

            var response = await _orderService.GetMyOrdersAsync(userId);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("shopOrders")]
        public async Task<IActionResult> ShopOrders([FromQuery] Guid shopId)
        {
            if (shopId == Guid.Empty)
            {
                return BadRequest(new { Message = "ShopId không hợp lệ." });
            }

            var response = await _orderService.GetShopOrdersAsync(shopId);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("all")]
        [Authorize] // Có thể thêm role check: [Authorize(Roles = "Admin")]
        public async Task<IActionResult> All()
        {
            var response = await _orderService.GetAllOrdersAsync();
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("updateStatus/{id}")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] OrderUpdateStatusDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            var response = await _orderService.UpdateStatusAsync(id, dto);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
