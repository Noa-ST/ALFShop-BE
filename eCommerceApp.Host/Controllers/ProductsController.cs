using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

            var result = await productService.AddAsync(dto);
            return result.Succeeded
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
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
    }
}