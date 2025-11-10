using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopsController : ControllerBase
    {
        private readonly IShopService _shopService;
        private readonly IOrderService _orderService;

        public ShopsController(IShopService shopService, IOrderService orderService)
        {
            _shopService = shopService;
            _orderService = orderService;
        }

        [HttpGet("getall-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllActive()
        {
            var data = await _shopService.GetAllActiveAsync();
            // ✅ Fix: Trả về 200 OK với empty array thay vì 404
            return Ok(data);
        }

        [HttpGet("seller/{sellerId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBySellerId(string sellerId)
        {
            var data = await _shopService.GetBySellerIdAsync(sellerId);
            // ✅ Fix: Trả về 200 OK với empty array thay vì 404
            return Ok(data);
        }

        [HttpGet("get-single/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await _shopService.GetByIdAsync(id);
            return data != null ? Ok(data) : NotFound(new { message = "Shop not found." });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Chỉ Seller và Admin mới được tạo shop
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateShop dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _shopService.CreateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Chỉ Seller và Admin mới được update shop
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShop dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ Fix: Validate id từ route với id từ body
            if (id != dto.Id)
            {
                return BadRequest(new { message = "Shop ID in route does not match ID in body." });
            }

            // ✅ Lấy userId từ JWT để validate ownership
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
            }

            var result = await _shopService.UpdateAsync(dto, userId);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("delete/{id:guid}")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Chỉ Seller và Admin mới được xóa shop
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // ✅ Lấy userId từ JWT để validate ownership
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
            }

            var result = await _shopService.DeleteAsync(id, userId);
            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ New: Search and filter with pagination
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<GetShop>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchAndFilter([FromQuery] ShopFilterDto filter)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _shopService.SearchAndFilterAsync(filter);
            return Ok(result);
        }

        // ✅ New: Rating Management
        [HttpPost("{id:guid}/recalculate-rating")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RecalculateRating(Guid id)
        {
            var result = await _shopService.RecalculateRatingAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ New: Statistics endpoint for Admin dashboard
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetStatistics()
        {
            var response = await _shopService.GetStatisticsAsync();
            return response.Succeeded
                ? Ok(response)
                : StatusCode((int)response.StatusCode, response);
        }

        // New: Revenue summary for a shop
        [HttpGet("{shopId:guid}/revenue/summary")]
        [Authorize(Roles = "Seller,Admin")]
        [ProducesResponseType(typeof(ServiceResponse<eCommerceApp.Aplication.DTOs.Order.ShopRevenueSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRevenueSummary(
            Guid shopId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? groupBy = "day",
            [FromQuery] bool onlyPaid = true,
            [FromQuery] string? status = "Delivered",
            [FromQuery] string[]? paymentMethod = null)
        {
            if (shopId == Guid.Empty)
            {
                return BadRequest(new { message = "ShopId không hợp lệ." });
            }

            // ✅ Ownership check: Seller chỉ được xem doanh thu của shop mình sở hữu
            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                var myShops = await _shopService.GetBySellerIdAsync(userId);
                var ownsShop = myShops?.Any(s => s.Id == shopId) == true;
                if (!ownsShop)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền truy cập doanh thu của shop này." });
                }
            }

            // Parse status & payment methods
            eCommerceApp.Domain.Enums.OrderStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<eCommerceApp.Domain.Enums.OrderStatus>(status, true, out var s))
            {
                parsedStatus = s;
            }

            IEnumerable<eCommerceApp.Domain.Enums.PaymentMethod>? methods = null;
            if (paymentMethod != null && paymentMethod.Length > 0)
            {
                var list = new List<eCommerceApp.Domain.Enums.PaymentMethod>();
                foreach (var m in paymentMethod)
                {
                    if (Enum.TryParse<eCommerceApp.Domain.Enums.PaymentMethod>(m, true, out var pm))
                    {
                        list.Add(pm);
                    }
                }
                methods = list;
            }

            var response = await _orderService.GetShopRevenueSummaryAsync(shopId, from, to, groupBy, onlyPaid, parsedStatus, methods);
            return StatusCode((int)response.StatusCode, response);
        }
    }
}
