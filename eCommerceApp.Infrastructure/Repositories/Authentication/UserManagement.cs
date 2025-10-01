using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces.Authentication;
using eCommerceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eCommerceApp.Infrastructure.Repositories.Authentication
{
    public class UserManagement(IRoleManagement roleManagement, UserManager<AppUser> userManager, AppDbContext context) : IUserManagement
    {
        public async Task<bool> CreateUser(AppUser user, string password)
        {
            var _user = await GetUserByEmail(user.Email!);
            if (_user != null) return false;

            var result = await userManager.CreateAsync(user, password);

            return result.Succeeded;
        }

        public async Task<IEnumerable<AppUser>?> GetAllUsers() => await context.Users.ToListAsync();


        public async Task<AppUser?> GetUserByEmail(string email) => await userManager.FindByEmailAsync(email);


        public async Task<AppUser> GetUserById(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            return user!;
        }

        public async Task<List<Claim>> GetUserClaims(string email)
        {
            var _user = await GetUserByEmail(email);

            string? roleName = await roleManagement.GetUserRole(_user!.Email!);

            List<Claim> claims = [
               new Claim("Fullname", _user!.FullName),
               new Claim(ClaimTypes. NameIdentifier, _user!.Id),
               new Claim(ClaimTypes. Email, _user!.Email!),
               new Claim(ClaimTypes. Role, roleName!)
            ];
            return claims;
        }

        public async Task<bool> LoginUser(AppUser user)
        {
            var _user = await GetUserByEmail(user.Email!);
            if (_user is null) return false;

            string? roleName = await roleManagement.GetUserRole(_user!.Email!);
            if (string.IsNullOrEmpty(roleName)) return false;

            return await userManager.CheckPasswordAsync(_user, user.PasswordHash!);
        }

        public async Task<int> RemoveUserByEmail(string email)
        {
            var user = await context.Users.FirstOrDefaultAsync(_ => _.Email == email);
            if (user == null)
            {
                return 0; 
            }
            context.Users.Remove(user);
            return await context.SaveChangesAsync();
        }
    }
}
