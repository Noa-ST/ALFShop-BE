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

    // Hàm tiện ích lấy User ID (string) từ JWT Claim
    private string GetUserId()
    {
        // NameIdentifier (UserId) được lưu dưới dạng string trong Identity
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    // POST /api/Cart/add (Thêm hoặc tăng số lượng sản phẩm)
    [HttpPost("add")]
    [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItem dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var response = await _cartService.AddItemToCartAsync(GetUserId(), dto);

        // Trả về StatusCode chính xác từ Service (200, 400, 404, etc.)
        return StatusCode((int)response.StatusCode, response);
    }

    // PUT /api/Cart/update (Cập nhật số lượng sản phẩm)
    [HttpPut("update")]
    [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItem dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var response = await _cartService.UpdateCartItemQuantityAsync(GetUserId(), dto);

        return StatusCode((int)response.StatusCode, response);
    }

    // DELETE /api/Cart/deleteItem/{productId} (Xóa một item khỏi giỏ)
    [HttpDelete("deleteItem/{productId:guid}")]
    [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> RemoveItem(Guid productId)
    {
        var response = await _cartService.RemoveItemFromCartAsync(GetUserId(), productId);

        return StatusCode((int)response.StatusCode, response);
    }

    // GET /api/Cart (Lấy chi tiết giỏ hàng hiện tại)
    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<GetCartDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetCart()
    {
        var response = await _cartService.GetCurrentCartAsync(GetUserId());

        return StatusCode((int)response.StatusCode, response);
    }
}