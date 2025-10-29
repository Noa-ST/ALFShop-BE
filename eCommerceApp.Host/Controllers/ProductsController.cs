using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Implementations;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IProductService productService) : ControllerBase
    {
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var data = await productService.GetAllAsync();
            return data.Any() ? Ok(data) : NotFound(new { message = "No products found." });
        }

        [HttpGet("getbyshop/{shopId:guid}")]
        public async Task<IActionResult> GetByShopId(Guid shopId)
        {
            var data = await productService.GetByShopIdAsync(shopId);
            return data.Any() ? Ok(data) : NotFound(new { message = "No products found for this shop." });
        }

        [HttpGet("getbycategory/{categoryId:guid}")]
        public async Task<IActionResult> GetByCategoryId(Guid categoryId)
        {
            var data = await productService.GetByGlobalCategoryIdAsync(categoryId);
            return data.Any() ? Ok(data) : NotFound(new { message = "No products found in this category." });
        }

        [HttpGet("detail/{id:guid}")]
        public async Task<IActionResult> GetDetailById(Guid id)
        {
            var data = await productService.GetDetailByIdAsync(id);
            return data != null ? Ok(data) : NotFound(new { message = "Product not found." });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateProduct dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await productService.AddAsync(dto);
                return result.Succeeded
                    ? Ok(new { message = result.Message })
                    : BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                // Log exception để debug
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", detail = ex.StackTrace });
            }
        }

        [HttpPut("update/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProduct dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await productService.UpdateAsync(id, dto);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }


        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await productService.DeleteAsync(id);
            return result.Succeeded
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        [HttpPut("reject/{id:guid}")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RejectProduct(
        Guid id,
        [FromQuery] string? rejectionReason = null)
        {
            var response = await productService.RejectProductAsync(id, rejectionReason);

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("approve/{id:guid}")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ApproveProduct(Guid id)
        {
            var response = await productService.ApproveProductAsync(id);

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("soft-delete/{id:guid}")]
        [ProducesResponseType(typeof(ServiceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await productService.DeleteAsync(id);
            return result.Succeeded
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }
    }
}