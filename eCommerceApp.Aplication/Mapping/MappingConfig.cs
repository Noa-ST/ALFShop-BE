using AutoMapper;
using eCommerceApp.Aplication.DTOs.Category;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Aplication.Mapping
{
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

            // Ánh xạ Shop -> GetShop (Toàn bộ thông tin Shop)
            CreateMap<Shop, GetShop>()
                .ForMember(dest => dest.SellerName,
                    opt => opt.MapFrom(src => src.Seller != null ? src.Seller.FullName : null))
                .ReverseMap();

            // ✅ Ánh xạ Shop -> ShopForProductDetail (Rút gọn)
            CreateMap<Shop, ShopForProductDetail>()
                .ForMember(dest => dest.LogoUrl, opt => opt.MapFrom(src => src.Logo))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.AverageRating));


            // --- PRODUCT ---

            // ProductImage <-> DTO
            CreateMap<ProductImage, ProductImageDto>().ReverseMap();

            // Create/Update Product (Giữ nguyên)
            CreateMap<CreateProduct, Product>()
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ProductStatus.Pending))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src =>
                    src.ImageUrls != null
                        ? src.ImageUrls.Select(url => new ProductImage
                        {
                            Url = url,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        }).ToList()
                        : new List<ProductImage>()));

            CreateMap<UpdateProduct, Product>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Images, opt => opt.Ignore()) // xử lý riêng ảnh trong service
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    src.Status.HasValue ? src.Status.Value : ProductStatus.Pending));

            // ✅ Cập nhật: Product → GetProduct
            CreateMap<Product, GetProduct>()
                .ForMember(dest => dest.ShopName, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Name : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))

                // Ánh xạ Shop lồng nhau
                .ForMember(dest => dest.Shop, opt => opt.MapFrom(src => src.Shop))

                // ✅ Ánh xạ ProductImages (Đã sửa để dùng List<ProductImage> rỗng khi không có ảnh)
                .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src =>
                    src.Images != null
                        ? src.Images.Where(i => !i.IsDeleted)
                        : new List<ProductImage>()
                )); // <-- Lỗi CS1061 đã được khắc phục bằng cách XÓA dòng ImageUrls bên dưới

            // Product → GetProductDetail (mở rộng từ GetProduct)
            CreateMap<Product, GetProductDetail>()
                .IncludeBase<Product, GetProduct>()
                .ForMember(dest => dest.ShopDescription, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Description : null))
                .ForMember(dest => dest.ShopLogo, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Logo : null))
                .ForMember(dest => dest.CategoryDescription, opt => opt.MapFrom(src => src.Category != null ? src.Category.Description : null));
        }
    }
}