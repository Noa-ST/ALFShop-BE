// File: eCommerceApp.Host/Controllers/CartController.cs

using eCommerceApp.Aplication.DTOs.Cart;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace eCommerceApp.Host.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập (Customer)
    [Route("api/[controller]")] // Route: /api/Cart
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // ✅ Hàm tiện ích lấy User ID (string) từ JWT Claim
        private string GetUserId()
        {
            // NameIdentifier (UserId) được lưu dưới dạng string trong Identity
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định người dùng. Vui lòng đăng nhập lại.");
            }
            return userId;
        }

        // POST /api/Cart/add (Thêm hoặc tăng số lượng sản phẩm)
        [HttpPost("add")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddItem([FromBody] AddCartItem dto)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var response = await _cartService.AddItemToCartAsync(userId, dto);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // PUT /api/Cart/update (Cập nhật số lượng sản phẩm)
        [HttpPut("update")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItem dto)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var response = await _cartService.UpdateCartItemQuantityAsync(userId, dto);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // DELETE /api/Cart/deleteItem/{productId} (Xóa một item khỏi giỏ)
        [HttpDelete("deleteItem/{productId:guid}")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            try
            {
                var userId = GetUserId();
                var response = await _cartService.RemoveItemFromCartAsync(userId, productId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // GET /api/Cart (Lấy chi tiết giỏ hàng hiện tại)
        [HttpGet]
        [ProducesResponseType(typeof(ServiceResponse<GetCartDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetUserId();
                var response = await _cartService.GetCurrentCartAsync(userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ New: Clear Cart endpoint
        [HttpDelete("clear")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetUserId();
                var response = await _cartService.ClearCartAsync(userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}