using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.Services.Interfaces.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eCommerceApp.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthencationController(IAuthenticationService authenticationService) : ControllerBase
    {
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser(CreateUser user)
        {
            var result = await authenticationService.CreateUser(user);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser(LoginUser user)
        {
            var result = await authenticationService.LoginUser(user);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ✅ SỬA: Đổi từ GET sang POST để bảo mật hơn
        [HttpPost("refresh")]
        public async Task<IActionResult> ReviveToken([FromBody] RefreshTokenRequest request)
        {
            var result = await authenticationService.ReviveToken(request.RefreshToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ✅ MỚI: Logout endpoint
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var result = await authenticationService.Logout(userId, request.RefreshToken);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        // ✅ MỚI: Lấy thông tin user hiện tại
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var result = await authenticationService.GetCurrentUser(userId);
            return result != null ? Ok(result) : NotFound();
        }

        // ✅ MỚI: Đổi mật khẩu
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var result = await authenticationService.ChangePassword(userId, request);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        // ✅ MỚI: Quên mật khẩu - Gửi email reset
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await authenticationService.ForgotPassword(request.Email);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        // ✅ MỚI: Reset mật khẩu với token
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await authenticationService.ResetPassword(request);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }


        // ✅ MỚI: Cập nhật profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var result = await authenticationService.UpdateProfile(userId, request);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
    }
}