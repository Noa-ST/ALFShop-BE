using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopsController(IShopService shopService) : ControllerBase
    {
        /// <summary>
        /// Get all active (non-deleted) shops
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetAllActive()
        {
            var data = await shopService.GetAllActiveAsync();
            return data.Any() ? Ok(data) : NotFound("No active shops found.");
        }

        /// <summary>
        /// Get all shops of a specific seller
        /// </summary>
        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetBySellerId(string sellerId)
        {
            var data = await shopService.GetBySellerIdAsync(sellerId);
            return data.Any() ? Ok(data) : NotFound("No shops found for this seller.");
        }

        /// <summary>
        /// Get a single shop by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await shopService.GetByIdAsync(id);
            return data != null ? Ok(data) : NotFound("Shop not found.");
        }

        /// <summary>
        /// Create a new shop
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateShop dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await shopService.CreateAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Update an existing shop
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateShop dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await shopService.UpdateAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Delete (soft delete) a shop by ID
        /// </summary>
        [HttpDelete("delete/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await shopService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
