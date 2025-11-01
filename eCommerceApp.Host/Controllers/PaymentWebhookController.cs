using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Aplication.Services.Interfaces;
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Webhook([FromBody] PayOSWebhookRequest webhook)
        {
            try
            {
                // ✅ Validation: Check webhook data
                if (webhook == null)
                {
                    return BadRequest(new { code = "01", desc = "Webhook data không hợp lệ." });
                }

                if (webhook.Data == null)
                {
                    return BadRequest(new { code = "01", desc = "Webhook data không hợp lệ." });
                }

                var result = await _paymentService.ProcessWebhookAsync(webhook);
                
                // PayOS yêu cầu trả về HTTP 200 với body { "code": "00", "desc": "Success" }
                // Ngay cả khi có lỗi, cũng nên trả về 200 để PayOS không retry
                if (result.Succeeded)
                {
                    return Ok(new { code = "00", desc = "Success" });
                }
                
                // ✅ Log error nhưng vẫn trả về 200 để PayOS không retry
                // TODO: Log error to logging service
                return Ok(new { code = "01", desc = result.Message });
            }
            catch (Exception ex)
            {
                // ✅ Log exception nhưng vẫn trả về 200 để PayOS không retry
                // TODO: Log exception to logging service
                return Ok(new { code = "99", desc = $"Internal error: {ex.Message}" });
            }
        }
    }
}

