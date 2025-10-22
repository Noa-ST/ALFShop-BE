using eCommerceApp.Aplication.DTOs.ShopCategory;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Cần thiết cho IEnumerable

// ✅ Chỉ Seller mới được truy cập
[Authorize(Roles = "Seller")]
[Route("api/Seller/[controller]")] // Route: /api/Seller/ShopCategory
[ApiController]
public class ShopCategoryController : ControllerBase
{
    private readonly IShopCategoryService _shopCategoryService;
    private readonly IShopService _shopService; // Cần Service để lấy ShopId

    public ShopCategoryController(IShopCategoryService shopCategoryService, IShopService shopService)
    {
        _shopCategoryService = shopCategoryService;
        _shopService = shopService;
    }

    // Hàm tiện ích để lấy Shop ID của Seller
    private async Task<Guid> GetShopIdAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // 1. Kiểm tra User ID
        if (string.IsNullOrEmpty(userIdClaim))
            return Guid.Empty;

        // 2. Chuyển đổi string (UserId) sang string (SellerId)
        // SellerId trong Entity Shop là string, nên dùng trực tiếp Claim value.
        string sellerIdString = userIdClaim;

        // 3. Gọi Service để lấy Shop DTO
        // Phương thức này trả về Task<IEnumerable<GetShop>>
        var shops = await _shopService.GetBySellerIdAsync(sellerIdString);

        // 4. Trích xuất ShopId: Vì mỗi Seller chỉ có 1 Shop, ta lấy phần tử đầu tiên
        var shop = shops?.FirstOrDefault();

        if (shop != null && shop.Id != Guid.Empty)
        {
            return shop.Id;
        }

        return Guid.Empty; // Nếu Seller chưa có Shop hoặc Shop không có ID hợp lệ
    }

    // POST /api/Seller/ShopCategory/create
    [HttpPost("create")]
    [ProducesResponseType(typeof(ServiceResponse<GetShopCategory>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateShopCategory dto)
    {
        var shopId = await GetShopIdAsync();
        if (shopId == Guid.Empty)
            // Trả về 403 Forbidden nếu không xác định được Shop của Seller
            return Unauthorized(ServiceResponse.Fail("Người dùng hiện tại không phải là Seller hoặc chưa có Shop.", HttpStatusCode.Forbidden));

        var response = await _shopCategoryService.CreateShopCategoryAsync(shopId, dto);

        // Sử dụng StatusCode từ ServiceResponse để trả về mã HTTP tương ứng
        return StatusCode((int)response.StatusCode, response);
    }

    // PUT /api/Seller/ShopCategory/update/{id}
    [HttpPut("update/{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShopCategory dto)
    {
        var shopId = await GetShopIdAsync();
        if (shopId == Guid.Empty)
            return Unauthorized(ServiceResponse.Fail("Người dùng hiện tại không phải là Seller hoặc chưa có Shop.", HttpStatusCode.Forbidden));

        var response = await _shopCategoryService.UpdateShopCategoryAsync(shopId, id, dto);

        return StatusCode((int)response.StatusCode, response);
    }

    // DELETE /api/Seller/ShopCategory/delete/{id}
    [HttpDelete("delete/{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var shopId = await GetShopIdAsync();
        if (shopId == Guid.Empty)
            return Unauthorized(ServiceResponse.Fail("Người dùng hiện tại không phải là Seller hoặc chưa có Shop.", HttpStatusCode.Forbidden));

        var response = await _shopCategoryService.DeleteShopCategoryAsync(shopId, id);

        return StatusCode((int)response.StatusCode, response);
    }

    // GET /api/Seller/ShopCategory/list
    [HttpGet("list")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<GetShopCategory>>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetList()
    {
        var shopId = await GetShopIdAsync();
        if (shopId == Guid.Empty)
            return Unauthorized(ServiceResponse.Fail("Người dùng hiện tại không phải là Seller hoặc chưa có Shop.", HttpStatusCode.Forbidden));

        var response = await _shopCategoryService.GetShopCategoriesByShopIdAsync(shopId);

        return StatusCode((int)response.StatusCode, response);
    }
}