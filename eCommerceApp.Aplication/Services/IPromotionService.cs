using eCommerceApp.Aplication.DTOs.Promotion;
using System.Threading.Tasks;

// Namespace này phải là "Services.Interfaces"
namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IPromotionService
    {
        // SỬA LỖI CS0738:
        // Hàm này BẮT BUỘC phải trả về Task<PromotionDto>
        Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto);
    }
}