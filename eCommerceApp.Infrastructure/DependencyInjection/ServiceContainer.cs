using eCommerceApp.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Infrastructure.Repositories;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Infrastructure.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using eCommerceApp.Infrastructure.Midleware;
using eCommerceApp.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using eCommerceApp.Domain.Interfaces.Authentication;
using eCommerceApp.Infrastructure.Repositories.Authentication;

namespace eCommerceApp.Infrastructure.DependencyInjection
{
    /// <summary>
    /// ServiceContainer chịu trách nhiệm gom nhóm và đăng ký tất cả service ở tầng Infrastructure vào Dependency Injection Container.
    /// </summary>
    public static class ServiceContainer
    {
        // Hàm mở rộng: dùng để đăng ký tất cả service tầng Infrastructure vào DI Container
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration config)
        {
            // 1. DbContext (SQL Server)
            // Kết nối database qua ConnectionString (Default)
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("Default"), sqlOptions =>
                {
                    // Chỉ rõ assembly chứa migration
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    // Retry khi kết nối DB fail
                    sqlOptions.EnableRetryOnFailure();
                }),
                ServiceLifetime.Scoped);


            // 3. Logging Adapter
            // Dùng Serilog làm logger cho toàn bộ app (thay vì ILogger mặc định)
            services.AddScoped(typeof(IAppLogger<>), typeof(SerilogLoggerAdapter<>));

            // 4. Identity + Roles
            // Thêm ASP.NET Identity (User + Role)
            services.AddDefaultIdentity<User>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true; 
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

                // Quy tắc password
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredUniqueChars = 1;
            })
            .AddRoles<IdentityRole>()                  
            .AddEntityFrameworkStores<AppDbContext>();  // Lưu user + role trong database AppDbContext

            // 5. JWT Authentication 
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
          });

            // 6. Custom Authentication Services            
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

            return services;
        }

        // Middleware tuỳ chỉnh
        public static IApplicationBuilder UseInfrastructureService(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>(); // Bắt lỗi global, trả JSON thay vì crash
            return app;
        }
    }
}

