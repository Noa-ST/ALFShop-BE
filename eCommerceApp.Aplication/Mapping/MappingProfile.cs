using AutoMapper;
using eCommerceApp.Aplication.DTOs.Order;
using eCommerceApp.Aplication.DTOs.Settlement;
using eCommerceApp.Domain.Entities;

namespace eCommerceApp.Aplication.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ✅ Fix: Add OrderItem mapping
            CreateMap<OrderItem, OrderItemResponseDTO>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.PriceAtPurchase ?? 0m))
                .ForMember(dest => dest.LineTotal, opt => opt.Ignore()); // Ignore vì là computed property

            CreateMap<Order, OrderResponseDTO>()
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
                .ForMember(dest => dest.IsPaid, opt => opt.MapFrom(src => src.PaymentStatus == eCommerceApp.Domain.Enums.PaymentStatus.Paid))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FullName : null))
                .ForMember(dest => dest.ShopName, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Name : null))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ReverseMap();

            // ✅ New: Settlement mappings
            CreateMap<SellerBalance, SellerBalanceDto>()
                .ReverseMap();

            CreateMap<Settlement, SettlementDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.Method.ToString()))
                .ReverseMap();

            CreateMap<OrderSettlement, OrderSettlementDto>()
                .ReverseMap();

            // ✅ New: Review mapping tối thiểu (chủ yếu dùng MappingConfig)
            CreateMap<Review, eCommerceApp.Aplication.DTOs.Review.GetReview>();
        }
    }
}
