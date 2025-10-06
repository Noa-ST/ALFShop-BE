using eCommerceApp.Aplication.DependencyInjection;
using eCommerceApp.Infrastructure.DependencyInjection;
using Serilog;

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
builder.Services.AddApplicationService();

builder.Services.AddControllers();

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- CORS ----
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithOrigins("https://localhost:7109")
              .AllowCredentials();
    });
});

// ---- Build app ----
try
{
    var app = builder.Build();

    app.UseCors();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // ⚠️ Quan trọng: auth đã đăng ký trong ServiceContainer,
    // nên ở đây chỉ cần UseAuthentication + UseAuthorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
