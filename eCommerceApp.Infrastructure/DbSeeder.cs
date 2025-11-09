using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace eCommerceApp.Infrastructure
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Admin", "Seller", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Cho phép cấu hình email/password qua appsettings, có giá trị mặc định
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            string adminEmail = configuration["Admin:Email"] ?? "admin@aifshop.com";
            string adminPassword = configuration["Admin:Password"] ?? "Admin@123"; // đáp ứng yêu cầu: digit + ký tự đặc biệt + chữ hoa

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Log.Information("Admin user seeded: {Email}", adminEmail);
                }
                else
                {
                    Log.Error("Admin seeding failed: {Errors}", string.Join(";", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}

