using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.Services.Interfaces.Authentication;
using eCommerceApp.Aplication.Validations;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;

namespace eCommerceApp.Aplication.Services.Implementations.Authentication
{
    public class AuthenticationService(
        ITokenManagement tokenManagement,
        IMapper mapper,
        IValidator<CreateUser> createUserValidator,
        IValidator<LoginUser> loginUserValidator,
        IValidationService validationService,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager
    ) : IAuthenticationService
    {
        // ... (Hàm CreateUser giữ nguyên)
        public async Task<ServiceResponse> CreateUser(CreateUser user)
        {
            var validation = await validationService.ValidateAsync(user, createUserValidator);
            if (!validation.Succeeded) return validation;

            var mappedModel = mapper.Map<User>(user);
            mappedModel.UserName = user.Email;

            var result = await userManager.CreateAsync(mappedModel, user.Password);
            if (!result.Succeeded)
            {
                return new ServiceResponse
                {
                    Message = string.Join(";", result.Errors.Select(e => e.Description))
                };
            }

            var roleToAssign = user.Role == "Seller" ? "Seller" : "Customer";
            if (!await roleManager.RoleExistsAsync(roleToAssign))
            {
                await roleManager.CreateAsync(new IdentityRole(roleToAssign));
            }

            var assignResult = await userManager.AddToRoleAsync(mappedModel, roleToAssign);
            if (!assignResult.Succeeded)
            {
                await userManager.DeleteAsync(mappedModel);
                return new ServiceResponse { Message = "Error occurred while assigning role" };
            }

            return new ServiceResponse { Succeeded = true, Message = "Account created!" };
        }

        // Đăng nhập
        public async Task<LoginResponse> LoginUser(LoginUser user)
        {
            var validation = await validationService.ValidateAsync(user, loginUserValidator);
            if (!validation.Succeeded)
                // Dùng Named Arguments cho tất cả 7 tham số
                return new LoginResponse(
                    Success: false,
                    Message: validation.Message ?? "",
                    Token: null!,
                    RefreshToken: null!,
                    Role: "",
                    UserId: "",
                    Fullname: "");

            var _user = await userManager.FindByEmailAsync(user.Email);
            if (_user == null)
                // Dùng Named Arguments cho tất cả 7 tham số
                return new LoginResponse(
                    Success: false,
                    Message: "Email not found",
                    Token: null!,
                    RefreshToken: null!,
                    Role: "",
                    UserId: "",
                    Fullname: "");

            var validPassword = await userManager.CheckPasswordAsync(_user, user.Password);
            if (!validPassword)
                // Dùng Named Arguments cho tất cả 7 tham số
                return new LoginResponse(
                    Success: false,
                    Message: "Invalid credentials",
                    Token: null!,
                    RefreshToken: null!,
                    Role: "",
                    UserId: "",
                    Fullname: "");

            var roles = await userManager.GetRolesAsync(_user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _user.Id.ToString()),
                new Claim(ClaimTypes.Email, _user.Email!),
                new Claim("Fullname", _user.FullName ?? "")
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            string jwtToken = tokenManagement.GenerateToken(claims);
            string refreshToken = tokenManagement.GetRefreshToken();

            int saveTokenResult = await tokenManagement.AddRefreshToken(_user.Id.ToString(), refreshToken);

            var mainRole = roles.FirstOrDefault() ?? "Customer";

            // Sử dụng Named Arguments cho tất cả 7 tham số trong mọi trường hợp trả về
            return saveTokenResult <= 0
                // Trường hợp lỗi lưu token
                ? new LoginResponse(
                    Success: false,
                    Message: "Internal error occurred while authenticating",
                    Token: jwtToken,
                    RefreshToken: refreshToken,
                    Role: mainRole,
                    UserId: _user.Id,
                    Fullname: _user.FullName ?? ""
                )
                // Trường hợp thành công
                : new LoginResponse(
                    Success: true,
                    Message: "Login successful",
                    Token: jwtToken,
                    RefreshToken: refreshToken,
                    Role: mainRole,
                    UserId: _user.Id,
                    Fullname: _user.FullName ?? ""
                );
        }

        // Làm mới token
        public async Task<LoginResponse> ReviveToken(string refreshToken)
        {
            bool validateTokenResult = await tokenManagement.ValidateRefreshToken(refreshToken);
            if (!validateTokenResult)
                return new LoginResponse(
                    Success: false,
                    Message: "Invalid token",
                    Token: null!,
                    RefreshToken: null!,
                    Role: "",
                    UserId: "",
                    Fullname: "");

            string userIdString = await tokenManagement.GetUserIdByRefreshToken(refreshToken);
            var user = await userManager.FindByIdAsync(userIdString);
            if (user == null)
                return new LoginResponse(
                    Success: false,
                    Message: "User not found",
                    Token: null!,
                    RefreshToken: null!,
                    Role: "",
                    UserId: "",
                    Fullname: "");

            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("Fullname", user.FullName ?? "")
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            string newJwtToken = tokenManagement.GenerateToken(claims);
            string newRefreshToken = tokenManagement.GetRefreshToken();

            await tokenManagement.UpdateRefreshToken(userIdString, newRefreshToken);

            var mainRole = roles.FirstOrDefault() ?? "Customer";

            // Trường hợp thành công
            return new LoginResponse(
                Success: true,
                Message: "Token refreshed successfully",
                Token: newJwtToken,
                RefreshToken: newRefreshToken,
                Role: mainRole,
                UserId: user.Id,
                Fullname: user.FullName ?? ""
            );
        }
    }
}