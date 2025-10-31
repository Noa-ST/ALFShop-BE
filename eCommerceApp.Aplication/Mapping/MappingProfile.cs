using AutoMapper;
using eCommerceApp.Aplication.DTOs.Order;
using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Aplication.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Order, OrderResponseDTO>()
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FullName : null))
                .ForMember(dest => dest.ShopName, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Name : null))
                .ReverseMap();
        }
    }
}
