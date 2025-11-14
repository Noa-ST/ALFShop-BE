// Top-level statements in Program.cs
using eCommerceApp.Aplication.DependencyInjection;
using eCommerceApp.Infrastructure.DependencyInjection;
using eCommerceApp.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection; // ✅ Thêm
using System.IO; // ✅ Thêm
using eCommerceApp.Infrastructure; // ✅ Gọi DbSeeder

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("log/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
Log.Logger.Information("Application is building...");

// ---- Application & Infrastructure DI ----
builder.Services.AddInfrastructureService(builder.Configuration);
builder.Services.AddApplicationServices();

// ✅ Đăng ký IHttpContextAccessor để sử dụng trong services
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    // ✅ Ensure Guid.Empty values are serialized (ShopId should always be included in JSON)
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
});

builder.Services.AddSignalR();

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AIFShop API", Version = "v1" });

    // ✅ BỔ SUNG CẤU HÌNH JWT SECURITY
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập 'Bearer ' theo sau là token JWT (Ví dụ: Bearer asd12345...)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() // ✅ thay vì new string[] {}
        }
    });
});
// ---- CORS ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithOrigins(
                  "http://localhost:5173",
                  "https://localhost:5173",
                  "https://aifshop-web.onrender.com"
              )
              .AllowCredentials();
    });
});

// ✅ Cấu hình Data Protection: phải thực hiện TRƯỚC khi Build để tránh ServiceCollection read-only
var dpKeysPath = builder.Configuration["DataProtection__KeysPath"] ?? "/data/protection-keys";
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("AIFShop-BE")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

// ---- Build app ----
try
{
    var app = builder.Build();

    // ✅ Seed tài khoản Admin và các vai trò nếu chưa tồn tại
    await DbSeeder.SeedAdminAsync(app.Services);

    // Bật Swagger cả Production nếu muốn healthCheckPath = /swagger
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(AllowFrontend);
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Serve static files from wwwroot so uploaded images are accessible
    app.UseStaticFiles();

    // ⚠️ Quan trọng: auth đã đăng ký trong ServiceContainer,
    // nên ở đây chỉ cần UseAuthentication + UseAuthorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");

    Log.Logger.Information("Application is running...");
    app.Run();
}
catch (Exception ex)
{
    Log.Logger.Error(ex, "Application failed to start....");
}
finally
{
    Log.CloseAndFlush();
}
