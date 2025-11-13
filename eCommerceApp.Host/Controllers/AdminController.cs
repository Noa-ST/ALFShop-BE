using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IUserService userService, ILogger<AdminController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: /api/admin/users - Danh sách tất cả users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await _userService.GetUsersAsync(page, pageSize, role, isActive, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AdminController] GetUsers error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi tải danh sách người dùng" });
            }
        }

        // GET: /api/admin/users/sellers - Danh sách sellers
        [HttpGet("users/sellers")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSellers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _userService.GetUsersAsync(page, pageSize, "Seller", null, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AdminController] GetSellers error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi tải danh sách người bán" });
            }
        }

        // GET: /api/admin/users/customers - Danh sách customers
        [HttpGet("users/customers")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _userService.GetUsersAsync(page, pageSize, "Customer", null, null);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AdminController] GetCustomers error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi tải danh sách khách hàng" });
            }
        }

        // PUT: /api/admin/users/{id}/status - Kích hoạt/vô hiệu hoá user
        [HttpPut("users/{id}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateUserStatusDto dto)
        {
            try
            {
                var result = await _userService.UpdateUserStatusAsync(id, dto.IsActive);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AdminController] UpdateUserStatus error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái người dùng" });
            }
        }

        // PUT: /api/admin/users/{id}/role - Thay đổi role
        [HttpPut("users/{id}/role")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateUserRoleDto dto)
        {
            try
            {
                var result = await _userService.UpdateUserRoleAsync(id, dto.Role);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AdminController] UpdateUserRole error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi cập nhật vai trò người dùng" });
            }
        }

        // DELETE: /api/admin/users/{id} - Xóa user (soft delete)
        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                return StatusCode((int)result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AdminController] DeleteUser error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi xóa người dùng" });
            }
        }
    }

    public class UpdateUserStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class UpdateUserRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }
}
