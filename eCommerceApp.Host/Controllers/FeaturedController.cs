using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Featured;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeaturedController : ControllerBase
    {
        // GET /api/Featured/debug
        [HttpGet("debug")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<FeaturedScoreDebugDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Debug([FromServices] IFeaturedService featuredService, [FromQuery] int topN = 20)
        {
            var res = await featuredService.GetDebugScoresAsync(topN);
            return StatusCode((int)res.StatusCode, res);
        }
    }
}