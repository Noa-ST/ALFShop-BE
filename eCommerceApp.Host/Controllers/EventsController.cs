using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Featured;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // allow tracking without auth if global auth required
    public class EventsController : ControllerBase
    {
        [HttpPost("click")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TrackClick([FromServices] IUnitOfWork uow, [FromBody] TrackEventRequest req)
        {
            var ev = BuildEvent(req, "click");
            await uow.FeaturedEvents.AddEventAsync(ev);
            await uow.SaveChangesAsync();
            return Ok(ServiceResponse<bool>.Success(true));
        }

        [HttpPost("impression")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TrackImpression([FromServices] IUnitOfWork uow, [FromBody] TrackEventRequest req)
        {
            var ev = BuildEvent(req, "impression");
            await uow.FeaturedEvents.AddEventAsync(ev);
            await uow.SaveChangesAsync();
            return Ok(ServiceResponse<bool>.Success(true));
        }

        [HttpPost("add-to-cart")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TrackAddToCart([FromServices] IUnitOfWork uow, [FromBody] TrackEventRequest req)
        {
            var ev = BuildEvent(req, "add_to_cart");
            await uow.FeaturedEvents.AddEventAsync(ev);
            await uow.SaveChangesAsync();
            return Ok(ServiceResponse<bool>.Success(true));
        }

        private FeaturedEvent BuildEvent(TrackEventRequest req, string eventType)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return new FeaturedEvent
            {
                EntityType = req.EntityType,
                EntityId = req.EntityId,
                EventType = eventType,
                UserId = string.IsNullOrEmpty(userId) ? null : userId,
                SessionId = req.SessionId,
                Device = req.Device,
                Region = req.Region,
                City = req.City,
                MetadataJson = req.MetadataJson,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}