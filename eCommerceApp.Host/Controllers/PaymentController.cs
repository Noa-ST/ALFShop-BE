using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("{orderId}/process")]
        [Authorize]
        public async Task<IActionResult> Process(Guid orderId, [FromBody] string method)
        {
            if (orderId == Guid.Empty)
            {
                return BadRequest(new { Message = "OrderId không hợp lệ." });
            }

            if (string.IsNullOrEmpty(method))
            {
                return BadRequest(new { Message = "Phương thức thanh toán không được để trống." });
            }

            var result = await _paymentService.ProcessPaymentAsync(orderId, method);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{paymentId}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(Guid paymentId, [FromBody] UpdatePaymentStatusRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (paymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "PaymentId không hợp lệ." });
            }

            var result = await _paymentService.UpdatePaymentStatusAsync(
                paymentId, 
                dto.Status, 
                changedBy: "User", 
                reason: dto.Reason);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("refund")]
        [Authorize]
        public async Task<IActionResult> Refund([FromBody] RefundRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentService.RefundAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{paymentId}/history")]
        [Authorize]
        public async Task<IActionResult> GetHistory(Guid paymentId)
        {
            if (paymentId == Guid.Empty)
            {
                return BadRequest(new { Message = "PaymentId không hợp lệ." });
            }

            var result = await _paymentService.GetPaymentHistoryAsync(paymentId);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
