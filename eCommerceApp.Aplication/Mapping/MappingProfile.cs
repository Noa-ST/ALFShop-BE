using AutoMapper;
using eCommerceApp.Application.DTOs.Order;
using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Order, OrderResponseDTO>().ReverseMap();
        }
    }
}
