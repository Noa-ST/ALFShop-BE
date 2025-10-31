using Microsoft.AspNetCore.Mvc;
using eCommerceApp.Application.DTOs.Order;
using eCommerceApp.Application.Services.Interfaces;

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] OrderCreateDTO dto)
            => Ok(await _orderService.CreateOrderAsync(dto));

        [HttpGet("myOrders")]
        public async Task<IActionResult> MyOrders([FromQuery] Guid customerId)
            => Ok(await _orderService.GetMyOrdersAsync(customerId));

        [HttpGet("shopOrders")]
        public async Task<IActionResult> ShopOrders([FromQuery] Guid shopId)
            => Ok(await _orderService.GetShopOrdersAsync(shopId));

        [HttpGet("all")]
        public async Task<IActionResult> All()
            => Ok(await _orderService.GetAllOrdersAsync());

        [HttpPut("updateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] OrderUpdateStatusDTO dto)
            => Ok(await _orderService.UpdateStatusAsync(id, dto));
    }
}
