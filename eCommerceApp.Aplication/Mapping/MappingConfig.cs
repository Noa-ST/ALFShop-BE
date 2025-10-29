using AutoMapper;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Aplication.DTOs.Shop;
using eCommerceApp.Aplication.DTOs.GlobalCategory; // ✅ Thêm Global Category DTO
using eCommerceApp.Aplication.DTOs.ShopCategory; // ✅ Thêm Shop Category DTO
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using System.Collections.Generic;
using eCommerceApp.Aplication.DTOs.Address;
using eCommerceApp.Aplication.DTOs.Cart; // Cần thiết cho List/IEnumerable

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
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                // ✅ [BỔ SUNG]: Ánh xạ các trường địa chỉ mới
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country ?? "Việt Nam"));

            CreateMap<UpdateShop, Shop>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                // ✅ [BỔ SUNG]: Ánh xạ các trường địa chỉ mới cho Update
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country ?? "Việt Nam"));

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
                // ✅ [SỬA]: Map CategoryId từ DTO sang GlobalCategoryId trong Entity
                .ForMember(dest => dest.GlobalCategoryId, opt => opt.MapFrom(src => src.CategoryId))
                // ✅ [SỬA]: Bỏ qua Images vì chúng ta xử lý thủ công trong ProductService để tránh duplicate Id
                .ForMember(dest => dest.Images, opt => opt.Ignore());

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

            // --- CART ---

            // CartItem Entity -> GetCartItemDto
            CreateMap<CartItem, GetCartItemDto>()
                // Bỏ qua các trường tính toán (ProductName, ShopName, UnitPrice, ItemTotal)
                // vì chúng được tính toán và ánh xạ thủ công trong CartService
                .ForMember(dest => dest.ProductName, opt => opt.Ignore())
                .ForMember(dest => dest.ShopName, opt => opt.Ignore())
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
                .ForMember(dest => dest.ItemTotal, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());

            // Cart Entity -> GetCartDto
            CreateMap<Cart, GetCartDto>()
                .ForMember(dest => dest.CartId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore()); // Tính thủ công trong Service

            // DTO -> CartItem Entity (Không cần vì ta thêm trực tiếp vào Items collection)
            // CreateMap<AddCartItem, CartItem>().ReverseMap(); 

            // --- ADDRESS ---

            // CreateAddress DTO -> Address Entity
            CreateMap<CreateAddress, Address>()
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                // Đảm bảo các trường Street, City, Country được ánh xạ
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // UpdateAddress DTO -> Address Entity
            CreateMap<UpdateAddress, Address>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Id đến từ URL
                .ReverseMap(); // ReverseMap hỗ trợ ánh xạ từ Entity -> DTO

            // Address Entity -> GetAddressDto
            CreateMap<Address, GetAddressDto>().ReverseMap();
        }
    }
}