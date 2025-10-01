using AutoMapper;
using eCommerceApp.Aplication.DTOs.Category;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.DTOs.Product;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Entities.Identity;

namespace eCommerceApp.Aplication.Mapping
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<CreateCategory, Category>();
            CreateMap<CreateProduct, Product>();

            CreateMap<UpdateProduct, Product>();
            CreateMap<UpdateCategory, Category>();

            CreateMap<Category, GetCategory>();
            CreateMap<Product, GetProduct>();

            CreateMap<CreateUser, User>();
            CreateMap<LoginUser, User>();
        }
    }
}
