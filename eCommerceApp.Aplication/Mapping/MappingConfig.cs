using AutoMapper;
using eCommerceApp.Aplication.DTOs.Category;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Entities.Identity;

namespace eCommerceApp.Aplication.Mapping
{
    /// <summary>
    /// Cấu hình ánh xạ giữa DTO và Entity trong toàn hệ thống.
    /// Được sử dụng bởi AutoMapper để tự động chuyển đổi dữ liệu giữa tầng Application và Domain.
    /// </summary>
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            // --- Category ---
            CreateMap<CreateCategory, Category>()
              .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
              .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
            CreateMap<UpdateCategory, Category>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
            CreateMap<Category, GetCategory>()
            .ForMember(dest => dest.ShopName, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Name : null))
            .ReverseMap();


            // --- User ---
            CreateMap<CreateUser, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));

            CreateMap<LoginUser, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));

            // --- Shop ---
            CreateMap<CreateShop, Shop>()
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<UpdateShop, Shop>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Shop, GetShop>()
                .ForMember(dest => dest.SellerName,
                    opt => opt.MapFrom(src => src.Seller != null ? src.Seller.FullName : null))
                .ReverseMap();
        }
    }
}
