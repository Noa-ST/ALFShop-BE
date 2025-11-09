using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Interfaces.Authentication;
using eCommerceApp.Domain.Repositories;
using eCommerceApp.Infrastructure.Data;
using eCommerceApp.Infrastructure.Midleware;
using eCommerceApp.Infrastructure.Repositories;
using eCommerceApp.Infrastructure.Repositories.Authentication;
using eCommerceApp.Infrastructure.Service;
using eCommerceApp.Infrastructure.Realtime;
using eCommerceApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("Default"), npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                }),
                ServiceLifetime.Scoped);

            services.AddScoped(typeof(IAppLogger<>), typeof(SerilogLoggerAdapter<>));

            services.AddDefaultIdentity<User>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredUniqueChars = 1;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                    ValidIssuer = config["JWT:Issuer"],
                    ValidAudience = config["JWT:Audience"],
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["JWT:Key"]!)
                    )
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

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
            services.AddScoped<ISettlementRepository, SettlementRepository>(); // ✅ New
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<ISellerBalanceRepository, SellerBalanceRepository>(); // ✅ New
            services.AddScoped<IReviewRepository, ReviewRepository>(); // ✅ New
            services.AddScoped<IChatRealtimeNotifier, ChatRealtimeNotifier>();
            services.AddScoped<IEmailService, EmailService>();

            // ✅ Register UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ✅ Register Background Services
            services.AddHostedService<RefreshTokenCleanupService>();
            services.AddHostedService<PaymentLinkExpirationService>(); // ✅ New: Auto-expire payment links
            services.AddHostedService<OrderExpirationService>(); // ✅ New: Auto-cancel unpaid orders

            return services;
        }

        public static IApplicationBuilder UseInfrastructureService(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            // ✅ Add rate limiting middleware for authentication endpoints
            // 5 requests per 60 seconds for login/create endpoints
            app.UseMiddleware<Midleware.RateLimitMiddleware>();
            return app;
        }
    }
}
