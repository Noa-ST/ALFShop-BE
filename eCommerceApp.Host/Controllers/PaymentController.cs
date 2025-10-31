using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Payment;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eCommerceApp.API.Controllers
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
        public async Task<IActionResult> Process(Guid orderId, [FromBody] string method)
        {
            var result = await _paymentService.ProcessPaymentAsync(orderId, method);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{paymentId}/status")]
        public async Task<IActionResult> UpdateStatus(Guid paymentId, [FromBody] UpdatePaymentStatusRequest dto)
        {
            var result = await _paymentService.UpdatePaymentStatusAsync(paymentId, dto.Status);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
