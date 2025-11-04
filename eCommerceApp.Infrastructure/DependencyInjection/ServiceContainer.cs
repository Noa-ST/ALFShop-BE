using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Application.Services.Interfaces;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces; // <-- CẦN CHO IUnitOfWork
using eCommerceApp.Domain.Interfaces.Authentication;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using eCommerceApp.Infrastructure.Midleware;
using eCommerceApp.Infrastructure.Repositories; // <-- CẦN CHO UnitOfWork
using eCommerceApp.Infrastructure.Repositories.Authentication;
using eCommerceApp.Infrastructure.Service;
using eCommerceApp.Infrastructure.Realtime;
using eCommerceApp.Infrastructure.Services;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;



namespace eCommerceApp.Infrastructure.DependencyInjection
{
    public static class ServiceContainer
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration config)
        {
            // ... (Tất cả code đăng ký DbContext, Identity, Authentication của bạn) ...

            // ... (toàn bộ code cũ) ...

            // --- ĐĂNG KÝ REPOSITORIES VÀ UNIT OF WORK ---

            // (Các Repositories cũ của bạn)
            services.AddScoped<IUserManagement, UserManagement>();
            services.AddScoped<ITokenManagement, TokenManagement>();
            services.AddScoped<IRoleManagement, RoleManagement>();
            services.AddScoped<IGlobalCategoryRepository, GlobalCategoryRepository>();
            services.AddScoped<IShopCategoryRepository, ShopCategoryRepository>();
            services.AddScoped<IShopRepository, ShopRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IChatRealtimeNotifier, ChatRealtimeNotifier>();
            services.AddScoped<IEmailService, EmailService>();

            // Repository MỚI của chúng ta
            services.AddScoped<IPromotionRepository, PromotionRepository>();

            // THÊM DÒNG NÀY (ĐÂY LÀ CHỖ ĐÚNG CỦA NÓ)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        public static IApplicationBuilder UseInfrastructureService(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}