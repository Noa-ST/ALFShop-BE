using eCommerceApp.Domain.Entities.Identity;
using System.Security.Claims;

namespace eCommerceApp.Domain.Interfaces.Authentication
{
    public interface IUserManagement
    {
        Task<bool> CreateUser(User user, string password);
        Task<bool> LoginUser(User user);
        Task<User?> GetUserByEmail(string email);
        Task<User?> GetUserById(string id);
        Task<IEnumerable<User>?> GetAllUsers();
        Task<int> RemoveUserByEmail(string email);
        Task<List<Claim>> GetUserClaims(string email);
    }
}
