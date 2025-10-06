using eCommerceApp.Aplication.DTOs.Category;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger) : ControllerBase
    {
        // GET: api/categories?shopId={shopId}
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? shopId = null)
        {
            try
            {
                var data = shopId.HasValue
                    ? await categoryService.GetByShopIdAsync(shopId.Value)
                    : await categoryService.GetAllAsync();

                if (!data.Any())
                {
                    logger.LogWarning("No categories found for shopId={shopId}", shopId);
                    return NotFound("No categories found.");
                }

                logger.LogInformation("Fetched {Count} categories", data.Count());
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while fetching categories.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await categoryService.GetByIdAsync(id);
            if (data is null)
            {
                logger.LogWarning("Category with id={id} not found", id);
                return NotFound($"Category with ID {id} not found.");
            }
            return Ok(data);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateCategory category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await categoryService.AddAsync(category);
            logger.LogInformation("Category create result: {Message}", result.Message);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PUT: api/categories/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategory category)
        {
            if (id != category.Id)
                return BadRequest("ID mismatch.");

            var result = await categoryService.UpdateAsync(category);
            logger.LogInformation("Category update result: {Message}", result.Message);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE: api/categories/{id} (soft delete)
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await categoryService.DeleteAsync(id);
            logger.LogInformation("Category delete result: {Message}", result.Message);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
