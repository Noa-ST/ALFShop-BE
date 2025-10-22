using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopsController(IShopService shopService) : ControllerBase
    {

        [HttpGet("getall-active")]
        public async Task<IActionResult> GetAllActive()
        {
            var data = await shopService.GetAllActiveAsync();
            return data.Any() ? Ok(data) : NotFound("No active shops found.");
        }

        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetBySellerId(string sellerId)
        {
            var data = await shopService.GetBySellerIdAsync(sellerId);
            return data.Any() ? Ok(data) : NotFound("No shops found for this seller.");
        }

        [HttpGet("get-single/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await shopService.GetByIdAsync(id);
            return data != null ? Ok(data) : NotFound("Shop not found.");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateShop dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await shopService.CreateAsync(dto);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShop dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await shopService.UpdateAsync(dto);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("delete/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await shopService.DeleteAsync(id);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}
