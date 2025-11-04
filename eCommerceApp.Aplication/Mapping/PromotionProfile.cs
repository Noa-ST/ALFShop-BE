using AutoMapper;
using eCommerceApp.Aplication.DTOs.Promotion;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums; 
namespace eCommerceApp.Aplication.Mapping
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            // Map từ Entity -> Dto
            CreateMap<Promotion, PromotionDto>()
                // Chuyển Enum (số) thành Tên (string) cho dễ đọc
                .ForMember(dest => dest.PromotionType,
                           opt => opt.MapFrom(src => src.PromotionType.ToString()));

            // Map từ Dto (Create) -> Entity
            CreateMap<CreatePromotionDto, Promotion>()
                // Bỏ qua việc map Id và CurrentUsageCount khi tạo mới
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentUsageCount, opt => opt.MapFrom(src => 0));
        }
    }
}