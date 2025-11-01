using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Implementations;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Hosting;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IProductService productService) : ControllerBase
    {
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var data = await productService.GetAllAsync();
            return Ok(data); // ✅ Fix: Trả về 200 OK với empty array thay vì 404
        }

        // ✅ New: Search and filter with pagination
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<GetProduct>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchAndFilter([FromQuery] ProductFilterDto filter)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await productService.SearchAndFilterAsync(filter);
            return Ok(result);
        }

        [HttpGet("getbyshop/{shopId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByShopId(Guid shopId)
        {
            var data = await productService.GetByShopIdAsync(shopId);
            return Ok(data); // ✅ Fix: Trả về 200 OK với empty array thay vì 404
        }

        [HttpGet("getbycategory/{categoryId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCategoryId(Guid categoryId)
        {
            var data = await productService.GetByGlobalCategoryIdAsync(categoryId);
            return Ok(data); // ✅ Fix: Trả về 200 OK với empty array thay vì 404
        }

        [HttpGet("detail/{id:guid}")]
        public async Task<IActionResult> GetDetailById(Guid id)
        {
            var data = await productService.GetDetailByIdAsync(id);
            return data != null ? Ok(data) : NotFound(new { message = "Product not found." });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Chỉ Seller và Admin mới được tạo sản phẩm
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateProduct dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // ✅ Lấy userId từ JWT để validate shop ownership
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

                var result = await productService.AddAsync(dto, userId);
                return result.Succeeded
                    ? Ok(new { message = result.Message })
                    : BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                // ✅ Fix: Chỉ expose StackTrace trong Development
                var errorDetail = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.StackTrace
                    : null;
                
                return StatusCode(500, new { 
                    message = "Internal server error occurred.", 
                    detail = errorDetail 
                });
            }
        }

        [HttpPut("update/{id:guid}")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Chỉ Seller và Admin mới được cập nhật sản phẩm
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProduct dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ Lấy userId từ JWT để validate shop ownership
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var result = await productService.UpdateAsync(id, dto, userId);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }


        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Chỉ Seller và Admin mới được xóa sản phẩm
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // ✅ Lấy userId từ JWT để validate shop ownership
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var result = await productService.DeleteAsync(id, userId);
            return result.Succeeded
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        [HttpPut("reject/{id:guid}")]
        [Authorize(Roles = "Admin")] // ✅ Fix: Chỉ Admin mới được từ chối sản phẩm
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RejectProduct(
        Guid id,
        [FromQuery] string? rejectionReason = null)
        {
            var response = await productService.RejectProductAsync(id, rejectionReason);

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("approve/{id:guid}")]
        [Authorize(Roles = "Admin")] // ✅ Fix: Chỉ Admin mới được duyệt sản phẩm
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ApproveProduct(Guid id)
        {
            var response = await productService.ApproveProductAsync(id);

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("soft-delete/{id:guid}")]
        [Authorize(Roles = "Seller,Admin")] // ✅ Fix: Chỉ Seller và Admin mới được soft delete
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            // ✅ Lấy userId từ JWT để validate shop ownership
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var result = await productService.DeleteAsync(id, userId);
            return result.Succeeded
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        // ✅ New: Stock Management
        [HttpPut("{id:guid}/stock")]
        [Authorize(Roles = "Seller,Admin")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });

            var result = await productService.UpdateStockQuantityAsync(id, dto.StockQuantity, userId);
            return result.Succeeded
                ? Ok(result)
                : BadRequest(result);
        }

        // ✅ New: Rating Management
        [HttpPost("{id:guid}/recalculate-rating")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RecalculateRating(Guid id)
        {
            var result = await productService.RecalculateRatingAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        // ✅ New: Admin Dashboard Features
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<GetProduct>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await productService.GetProductsByStatusAsync(ProductStatus.Pending, page, pageSize);
            return Ok(result);
        }

        [HttpGet("admin/rejected")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<GetProduct>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRejectedProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await productService.GetProductsByStatusAsync(ProductStatus.Rejected, page, pageSize);
            return Ok(result);
        }

        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetStatistics()
        {
            var statistics = await productService.GetProductStatisticsAsync();
            return Ok(statistics);
        }
    }
}