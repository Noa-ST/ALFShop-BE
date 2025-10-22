using AutoMapper;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.DTOs.GlobalCategory; // ✅ Thêm Global Category DTO
using eCommerceApp.Aplication.DTOs.ShopCategory; // ✅ Thêm Shop Category DTO
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.Collections.Generic; // Cần thiết cho List/IEnumerable

namespace eCommerceApp.Aplication.Mapping
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            // --- GLOBAL CATEGORY (Thay thế Category cũ) ---

            // 💡 [SỬA ĐỔI]: Category cũ đã bị xóa. Thay bằng GlobalCategory.
            CreateMap<CreateGlobalCategory, GlobalCategory>()
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ReverseMap();

            CreateMap<UpdateGlobalCategory, GlobalCategory>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<GlobalCategory, GetGlobalCategory>()
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.Parent))
                .ReverseMap();

            // --- SHOP CATEGORY (Danh mục của Seller) ---
            CreateMap<CreateShopCategory, ShopCategory>()
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateShopCategory, ShopCategory>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<ShopCategory, GetShopCategory>()
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

            // Ánh xạ Shop -> ShopForProductDetail (Rút gọn)
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
                // ✅ [SỬA]: Product Entity dùng ProductImages (Navigation Property)
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
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    src.Status.HasValue ? src.Status.Value : ProductStatus.Pending));

            // ✅ Cập nhật: Product → GetProduct
            CreateMap<Product, GetProduct>()
                .ForMember(dest => dest.ShopName, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Name : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.GlobalCategory != null ? src.GlobalCategory.Name : null))

                // Ánh xạ Shop lồng nhau
                .ForMember(dest => dest.Shop, opt => opt.MapFrom(src => src.Shop))

                // Ánh xạ ProductImages
                .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src =>
                    src.Images != null 
                        ? src.Images.Where(i => !i.IsDeleted)
                        : new List<ProductImage>()));

            // Product → GetProductDetail (mở rộng từ GetProduct)
            CreateMap<Product, GetProductDetail>()
                .IncludeBase<Product, GetProduct>()
                .ForMember(dest => dest.ShopDescription, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Description : null))
                .ForMember(dest => dest.ShopLogo, opt => opt.MapFrom(src => src.Shop != null ? src.Shop.Logo : null))
                // ✅ [SỬA]: CategoryDescription cũ -> GlobalCategory.Description mới
                .ForMember(dest => dest.CategoryDescription, opt => opt.MapFrom(src => src.GlobalCategory != null ? src.GlobalCategory.Description : null));
        }
    }
}