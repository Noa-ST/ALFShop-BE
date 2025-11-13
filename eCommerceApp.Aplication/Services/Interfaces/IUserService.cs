using eCommerceApp.Aplication.DTOs;

namespace eCommerceApp.Aplication.Services.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<object>> GetUsersAsync(int page, int pageSize, string? role = null, bool? isActive = null, string? search = null);
        Task<ServiceResponse> UpdateUserStatusAsync(string userId, bool isActive);
        Task<ServiceResponse> UpdateUserRoleAsync(string userId, string newRole);
        Task<ServiceResponse> DeleteUserAsync(string userId);
    }
}
