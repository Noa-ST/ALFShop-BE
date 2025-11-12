using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Featured;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/Admin/featured")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminFeaturedController : ControllerBase
    {
        [HttpPost("pin")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Pin([FromServices] IFeaturedService featuredService, [FromBody] FeaturedPinRequest request)
        {
            var res = await featuredService.PinAsync(request);
            return StatusCode((int)res.StatusCode, res);
        }

        // ✅ Basic stats endpoint
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ServiceResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats(
            [FromServices] IUnitOfWork uow,
            [FromQuery] string? entityType = null,
            [FromQuery] Guid? entityId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int topN = 10)
        {
            object payload;

            if (!string.IsNullOrWhiteSpace(entityType) && entityId.HasValue && entityId.Value != Guid.Empty)
            {
                var totalsForEntity = await uow.FeaturedEvents.GetTotalsForEntityAsync(entityType!, entityId.Value, from, to);
                payload = new
                {
                    entityType,
                    entityId,
                    totalsForEntity.Clicks,
                    totalsForEntity.Impressions,
                    totalsForEntity.AddsToCart
                };
            }
            else
            {
                var totals = await uow.FeaturedEvents.GetTotalsAsync(entityType, from, to);

                List<(Guid EntityId, int Clicks, int Impressions, int AddsToCart)> top;
                int? totalCount = null;
                if (!string.IsNullOrWhiteSpace(entityType))
                {
                    if (pageSize > 0)
                    {
                        var paged = await uow.FeaturedEvents.GetTopEntitiesPagedAsync(entityType!, from, to, page, pageSize);
                        top = paged.Items;
                        totalCount = paged.TotalCount;
                    }
                    else
                    {
                        top = await uow.FeaturedEvents.GetTopEntitiesAsync(entityType!, from, to, topN);
                    }
                }
                else
                {
                    top = new List<(Guid, int, int, int)>();
                }

                payload = new
                {
                    totals.Clicks,
                    totals.Impressions,
                    totals.AddsToCart,
                    top,
                    totalCount
                };
            }

            return Ok(ServiceResponse<object>.Success(payload));
        }
    }
}