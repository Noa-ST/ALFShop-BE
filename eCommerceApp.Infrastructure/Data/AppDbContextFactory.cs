using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace eCommerceApp.Infrastructure.Data
{
    /// <summary>
    /// Design-time factory for EF Core tools to create AppDbContext without bootstrapping the web host.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Default")
                                     ?? "Host=localhost;Database=AIFShopBE;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
            });

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}