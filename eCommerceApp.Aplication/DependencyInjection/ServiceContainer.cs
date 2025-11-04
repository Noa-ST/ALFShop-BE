using eCommerceApp.Aplication.Services;
using eCommerceApp.Aplication.Services.Implementations;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;

namespace eCommerceApp.Aplication.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 1. Đăng ký AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // 2. Đăng ký Validator
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // 3. Đăng ký CÁC SERVICE CỦA APLICATION
            services.AddScoped<IPromotionService, PromotionService>(); // ✅ Đã sửa
            services.AddScoped<IProductService, ProductService>(); // ✅ Đã sửa
            services.AddScoped<IGlobalCategoryService, GlobalCategoryService>(); // ✅ Đã sửa
            services.AddScoped<ICartService, CartService>(); // ✅ Đã sửa

            // TẠM THỜI COMMENT LẠI CÁC DÒNG BỊ LỖI
            // services.AddScoped<IConversationService, ConversationService>();
            // services.AddScoped<IPaymentService, PaymentService>();

            // services.AddScoped<IOrderService, OrderService>();
            // services.AddScoped<IPayOSService, PayOSService>();

            return services;
        }
    }
}