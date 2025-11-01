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

        public ShopsController(IShopService shopService)
        {
            _shopService = shopService;
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
    }
}
