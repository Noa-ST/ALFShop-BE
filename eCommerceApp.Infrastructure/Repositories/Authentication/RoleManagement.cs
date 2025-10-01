using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;

namespace eCommerceApp.Infrastructure.Repositories.Authentication
{
    public class RoleManagement(UserManager<User> userManager) : IRoleManagement
    {
        public async Task<bool> AddUserToRole(User user, string roleName) => (await userManager.AddToRoleAsync(user, roleName)).Succeeded;
        public async Task<string?> GetUserRole(string userEmail)
        {
            var user = await userManager.FindByEmailAsync(userEmail);

            return (await userManager.GetRolesAsync(user!)).FirstOrDefault();
        }
    }
}
