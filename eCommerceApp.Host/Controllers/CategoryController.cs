using eCommerceApp.Aplication.DTOs.Category;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ICategoryService categoryService) : ControllerBase
    {
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await categoryService.GetAllAsync();
            return data.Any() ? Ok(data) : NotFound(data);
        }

        [HttpGet("single/{id}")]
        public async Task<IActionResult> GetBySingle(Guid id)
        {
            var data = await categoryService.GetByIdAsync(id);
            return data != null ? Ok(data) : NotFound(data);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add(CreateCategory category)
        {
            var result = await categoryService.AddAsync(category);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(UpdateCategory category)
        {
            var result = await categoryService.UpdateAsync(category);
            return result.Success ? Ok(result) : BadRequest(result);
        }


        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await categoryService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
