using eCommerceApp.Aplication.DTOs.GlobalCategory;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Hosting;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")] // ✅ Fix: Route hợp lý hơn - /api/GlobalCategory
    [ApiController]
    public class GlobalCategoryController : ControllerBase
    {
        private readonly IGlobalCategoryService _globalCategoryService;

        public GlobalCategoryController(IGlobalCategoryService globalCategoryService)
        {
            _globalCategoryService = globalCategoryService;
        }

        // ✅ Get category by ID
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ServiceResponse<GetGlobalCategory>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _globalCategoryService.GetGlobalCategoryByIdAsync(id);
            return response.Succeeded
                ? Ok(response)
                : StatusCode((int)response.StatusCode, response);
        }

        // ✅ Get categories by parent ID (null = root categories)
        [HttpGet("by-parent")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<GetGlobalCategory>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByParentId([FromQuery] Guid? parentId)
        {
            var response = await _globalCategoryService.GetCategoriesByParentIdAsync(parentId);
            return Ok(response);
        }

        // ✅ New: Get descendant IDs for a category
        [HttpGet("{id:guid}/descendants")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<Guid>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDescendantIds(Guid id, [FromQuery] bool includeSelf = false)
        {
            var response = await _globalCategoryService.GetDescendantIdsAsync(id, includeSelf);
            return response.Succeeded
                ? Ok(response)
                : StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin")] // ✅ Fix: Chỉ Admin mới được tạo category
        [ProducesResponseType(typeof(ServiceResponse<GetGlobalCategory>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateGlobalCategory dto)
        {
            // ✅ Fix: Thêm ModelState validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _globalCategoryService.CreateGlobalCategoryAsync(dto);

            return response.Succeeded
                ? Ok(response)
                : StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("update/{id:guid}")]
        [Authorize(Roles = "Admin")] // ✅ Fix: Chỉ Admin mới được update
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateGlobalCategory dto)
        {
            // ✅ Fix: Thêm ModelState validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _globalCategoryService.UpdateGlobalCategoryAsync(id, dto);

            return response.Succeeded
                ? Ok(response)
                : StatusCode((int)response.StatusCode, response);
        }

        [HttpDelete("delete/{id:guid}")]
        [Authorize(Roles = "Admin")] // ✅ Fix: Chỉ Admin mới được xóa
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var response = await _globalCategoryService.DeleteGlobalCategoryAsync(id);

            return response.Succeeded
                ? Ok(response)
                : StatusCode((int)response.StatusCode, response);
        }


        [HttpGet("all")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<GetGlobalCategory>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeChildren = true)
        {
            try
            {
                var response = await _globalCategoryService.GetAllGlobalCategoriesAsync(includeChildren);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // ✅ Fix: Chỉ expose StackTrace trong Development
                var errorDetail = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.StackTrace
                    : null;

                return StatusCode(500, new
                {
                    message = "Internal server error occurred.",
                    detail = errorDetail
                });
            }
        }

        // ✅ New: Statistics endpoint for Admin dashboard
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetStatistics()
        {
            var response = await _globalCategoryService.GetStatisticsAsync();
            return response.Succeeded
                ? Ok(response)
                : StatusCode((int)response.StatusCode, response);
        }

        // GET /api/Categories/featured?limit=6&region=VN
        [HttpGet("/api/Categories/featured")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<eCommerceApp.Aplication.DTOs.Featured.FeaturedCategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatured([FromServices] IFeaturedService featuredService, [FromQuery] int limit = 6, [FromQuery] string? region = null)
        {
            var data = await featuredService.GetFeaturedCategoriesAsync(limit, region);
            return Ok(ServiceResponse<IEnumerable<eCommerceApp.Aplication.DTOs.Featured.FeaturedCategoryDto>>.Success(data));
        }
    }
}