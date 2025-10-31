using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.Host.Controllers
{
    /// <summary>
    /// Controller để nhận webhook từ PayOS
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentWebhookController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("webhook")]
        [AllowAnonymous] // PayOS sẽ gọi từ bên ngoài, không cần authentication
        public async Task<IActionResult> Webhook([FromBody] PayOSWebhookRequest webhook)
        {
            try
            {
                var result = await _paymentService.ProcessWebhookAsync(webhook);
                
                // PayOS yêu cầu trả về HTTP 200 với body { "code": "00", "desc": "Success" }
                if (result.Succeeded)
                {
                    return Ok(new { code = "00", desc = "Success" });
                }
                
                return BadRequest(new { code = "01", desc = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "99", desc = $"Internal error: {ex.Message}" });
            }
        }
    }
}

