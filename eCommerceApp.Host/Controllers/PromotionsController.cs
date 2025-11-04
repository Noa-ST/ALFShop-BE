using eCommerceApp.Aplication.Services;
using eCommerceApp.Aplication.DTOs.Promotion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System; // <-- Thêm

namespace eCommerceApp.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Admin")] // Tạm thời comment lại để bạn test API cho dễ
    public class PromotionsController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionsController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromotionDto createDto)
        {
            // Service sẽ ném lỗi ValidationException nếu DTO không hợp lệ
            var newPromotion = await _promotionService.CreatePromotionAsync(createDto);

            // Trả về 201 Created, kèm theo thông tin của promotion vừa tạo
            // và đường dẫn API để lấy nó (ví dụ: api/Promotions/GUID-...)
            return CreatedAtAction(
                nameof(GetPromotionById), // Tên của hàm [HttpGet("{id}")]
                new { id = newPromotion.Id },
                newPromotion);
        }

        // Bạn cần tạo hàm này để CreatedAtAction ở trên hoạt động
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotionById(Guid id)
        {
            // (Bạn sẽ implement logic này trong PromotionService sau)
            // var promotion = await _promotionService.GetByIdAsync(id);
            // if (promotion == null) return NotFound();

            // Trả về tạm
            return Ok($"Đã gọi GetPromotionById với Id: {id}");
        }
    }
}