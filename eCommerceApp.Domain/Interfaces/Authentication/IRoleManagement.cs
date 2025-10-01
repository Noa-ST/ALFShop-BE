using eCommerceApp.Domain.Entities.Identity;

namespace eCommerceApp.Domain.Interfaces.Authentication
{
    public interface IRoleManagement
    {
        Task<string?> GetUserRole(string userEmail);
        Task<bool> AddUserToRole(User user, string roleName);
    }
}
