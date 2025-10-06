using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.Services.Interfaces.Authentication;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Aplication.Validations;
using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

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
        // Đăng ký tài khoản Customer hoặc Seller
        public async Task<ServiceResponse> CreateUser(CreateUser user)
        {
            var validation = await validationService.ValidateAsync(user, createUserValidator);
            if (!validation.Success) return validation;

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

            // Xác định role (Admin seed sẵn, chỉ Customer hoặc Seller được tạo qua API)
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

            return new ServiceResponse { Success = true, Message = "Account created!" };
        }

        // Đăng nhập
        public async Task<LoginResponse> LoginUser(LoginUser user)
        {
            var validation = await validationService.ValidateAsync(user, loginUserValidator);
            if (!validation.Success)
                return new LoginResponse(Message: validation.Message);

            var _user = await userManager.FindByEmailAsync(user.Email);
            if (_user == null)
                return new LoginResponse(Message: "Email not found");

            var validPassword = await userManager.CheckPasswordAsync(_user, user.Password);
            if (!validPassword)
                return new LoginResponse(Message: "Invalid credentials");

            // Claims
            var roles = await userManager.GetRolesAsync(_user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _user.Id),
                new Claim(ClaimTypes.Email, _user.Email!),
                new Claim("Fullname", _user.FullName ?? "")
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            string jwtToken = tokenManagement.GenerateToken(claims);
            string refreshToken = tokenManagement.GetRefreshToken();

            int saveTokenResult = await tokenManagement.AddRefreshToken(_user.Id, refreshToken);

            return saveTokenResult <= 0
                ? new LoginResponse(Message: "Internal error occurred while authenticating")
                : new LoginResponse(Success: true, Token: jwtToken, RefreshToken: refreshToken);
        }

        // Làm mới token
        public async Task<LoginResponse> ReviveToken(string refreshToken)
        {
            bool validateTokenResult = await tokenManagement.ValidateRefreshToken(refreshToken);
            if (!validateTokenResult)
                return new LoginResponse(Message: "Invalid token");

            string userId = await tokenManagement.GetUserIdByRefreshToken(refreshToken);
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return new LoginResponse(Message: "User not found");

            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("Fullname", user.FullName ?? "")
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            string newJwtToken = tokenManagement.GenerateToken(claims);
            string newRefreshToken = tokenManagement.GetRefreshToken();

            await tokenManagement.UpdateRefreshToken(userId, newRefreshToken);

            return new LoginResponse(Success: true, Token: newJwtToken, RefreshToken: newRefreshToken);
        }
    }
}
