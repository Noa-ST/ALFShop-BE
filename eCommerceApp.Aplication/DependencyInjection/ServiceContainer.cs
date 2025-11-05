using eCommerceApp.Aplication.Mapping;
using eCommerceApp.Aplication.Services.Implementations;
using eCommerceApp.Aplication.Services.Implementations.Authentication;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Authentication;
using eCommerceApp.Aplication.Validations;
using eCommerceApp.Aplication.Validations.Authentication;
using eCommerceApp.Domain.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerceApp.Aplication.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingConfig));
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IGlobalCategoryService, GlobalCategoryService>();
            services.AddScoped<IShopService, ShopService>();
            services.AddScoped<IShopCategoryService, ShopCategoryService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IAddressService, AddressService>();

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ISettlementService, SettlementService>(); // ✅ New
            services.AddScoped<IConversationService, ConversationService>();

            // PayOS Service - Đăng ký với HttpClient để hỗ trợ HTTP calls
            services.AddHttpClient<IPayOSService, PayOSService>();

            return services;
        }
    }
}
