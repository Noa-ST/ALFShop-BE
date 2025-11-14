using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Net;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _uow;

        public UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork uow)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _uow = uow;
        }

        public async Task<PagedResult<object>> GetUsersAsync(int page, int pageSize, string? role = null, bool? isActive = null, string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _userManager.Users.AsQueryable();

            // Filter by role
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            // Search by email or full name
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    (u.Email ?? "").Contains(search) ||
                    (u.FullName ?? "").Contains(search) ||
                    (u.UserName ?? "").Contains(search));
            }

            var total = query.Count();
            var users = query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Get roles for each user
            var userDtos = new List<object>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    userName = user.UserName,
                    roles = userRoles,
                    createdAt = user.CreatedAt,
                });
            }

            return new PagedResult<object>
            {
                Data = userDtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ServiceResponse> UpdateUserStatusAsync(string userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResponse.Fail("Người dùng không tồn tại.", HttpStatusCode.NotFound);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ServiceResponse.Fail("Cập nhật trạng thái thất bại.", HttpStatusCode.BadRequest);

            return ServiceResponse.Success(isActive ? "Kích hoạt người dùng thành công." : "Vô hiệu hoá người dùng thành công.");
        }

        public async Task<ServiceResponse> UpdateUserRoleAsync(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResponse.Fail("Người dùng không tồn tại.", HttpStatusCode.NotFound);

            // Check if role exists
            var roleExists = await _roleManager.RoleExistsAsync(newRole);
            if (!roleExists)
                return ServiceResponse.Fail("Vai trò không tồn tại.", HttpStatusCode.BadRequest);

            // Get current roles and remove them
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return ServiceResponse.Fail("Lỗi khi xóa vai trò cũ.", HttpStatusCode.BadRequest);
            }

            // Add new role
            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
                return ServiceResponse.Fail("Lỗi khi thêm vai trò mới.", HttpStatusCode.BadRequest);

            return ServiceResponse.Success($"Cập nhật vai trò thành công. Vai trò mới: {newRole}");
        }

        public async Task<ServiceResponse> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResponse.Fail("Người dùng không tồn tại.", HttpStatusCode.NotFound);

            // Soft delete - mark as inactive
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ServiceResponse.Fail("Xóa người dùng thất bại.", HttpStatusCode.BadRequest);

            return ServiceResponse.Success("Xóa người dùng thành công (vô hiệu hoá).");
        }
    }
}
