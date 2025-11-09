using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Identity;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Authentication;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
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
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        IValidationService validationService,
        IEmailService emailService,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IAppLogger<AuthenticationService> logger
    ) : IAuthenticationService
    {
        // ... (Hàm CreateUser giữ nguyên)
        public async Task<ServiceResponse> CreateUser(CreateUser user)
        {
            logger.LogInformation($"Attempting to create user with email: {user.Email}");
            
            var validation = await validationService.ValidateAsync(user, createUserValidator);
            if (!validation.Succeeded)
            {
                logger.LogWarning($"User creation validation failed for email: {user.Email}");
                return validation;
            }

            var mappedModel = mapper.Map<User>(user);
            mappedModel.UserName = user.Email;

            var result = await userManager.CreateAsync(mappedModel, user.Password);
            if (!result.Succeeded)
            {
                logger.LogError(new Exception($"User creation failed: {string.Join(";", result.Errors.Select(e => e.Description))}"), 
                    $"Failed to create user with email: {user.Email}");
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

            // Send email confirmation
            var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(mappedModel);
            _ = emailService.SendEmailConfirmationEmailAsync(
                mappedModel.Email!,
                emailConfirmationToken,
                mappedModel.FullName ?? mappedModel.Email!
            );

            logger.LogInformation($"User created successfully with email: {user.Email}, role: {roleToAssign}");
            return new ServiceResponse { Succeeded = true, Message = "Account created! Please check your email to confirm your account." };
        }

        // Đăng nhập
        public async Task<LoginResponse> LoginUser(LoginUser user)
        {
            logger.LogInformation($"Login attempt for email: {user.Email}");
            
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

            // Kiểm tra email đã được xác nhận chưa
            if (!_user.EmailConfirmed)
            {
                logger.LogWarning($"Login attempt failed - email not confirmed for: {user.Email}");
                return new LoginResponse(
                    Success: false,
                    Message: "Please confirm your email before logging in. Check your email for confirmation link.",
                    Token: null!,
                    RefreshToken: null!,
                    Role: "",
                    UserId: "",
                    Fullname: "");
            }

            var validPassword = await userManager.CheckPasswordAsync(_user, user.Password);
            if (!validPassword)
            {
                logger.LogWarning($"Login attempt failed - invalid password for: {user.Email}");
                // Dùng Named Arguments cho tất cả 7 tham số
                return new LoginResponse(
                    Success: false,
                    Message: "Invalid credentials",
                    Token: null!,
                    RefreshToken: null!,
                    Role: "",
                    UserId: "",
                    Fullname: "");
            }

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
            if (saveTokenResult <= 0)
            {
                logger.LogError(new Exception("Failed to save refresh token"), 
                    $"Failed to save refresh token for user: {_user.Email}");
                return new LoginResponse(
                    Success: false,
                    Message: "Internal error occurred while authenticating",
                    Token: jwtToken,
                    RefreshToken: refreshToken,
                    Role: mainRole,
                    UserId: _user.Id,
                    Fullname: _user.FullName ?? ""
                );
            }

            logger.LogInformation($"Login successful for user: {user.Email}, role: {mainRole}");
            return new LoginResponse(
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
            string newRefreshToken = await tokenManagement.UpdateRefreshTokenAndGetNew(userIdString, refreshToken);

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

        // Logout
        public async Task<ServiceResponse> Logout(string userId, string refreshToken)
        {
            logger.LogInformation($"Logout attempt for user ID: {userId}");
            var result = await tokenManagement.RevokeRefreshToken(refreshToken);
            if (result <= 0)
            {
                logger.LogWarning($"Failed to revoke refresh token for user ID: {userId}");
                return new ServiceResponse { Message = "Failed to logout" };
            }
            logger.LogInformation($"Logout successful for user ID: {userId}");
            return new ServiceResponse { Succeeded = true, Message = "Logged out successfully" };
        }

        // Get current user info
        public async Task<UserInfoDto?> GetCurrentUser(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await userManager.GetRolesAsync(user);
            return new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };
        }

        // Change password
        public async Task<ServiceResponse> ChangePassword(string userId, ChangePasswordRequest request)
        {
            var validation = await validationService.ValidateAsync(request, changePasswordValidator);
            if (!validation.Succeeded) return validation;

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResponse { Message = "User not found" };
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return new ServiceResponse
                {
                    Message = string.Join(";", result.Errors.Select(e => e.Description))
                };
            }

            return new ServiceResponse { Succeeded = true, Message = "Password changed successfully" };
        }

        // Forgot password - Send reset token
        public async Task<ServiceResponse> ForgotPassword(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if email exists for security
                return new ServiceResponse { Succeeded = true, Message = "If email exists, password reset link has been sent" };
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var emailSent = await emailService.SendPasswordResetEmailAsync(
                user.Email!,
                token,
                user.FullName ?? user.Email!
            );

            if (!emailSent)
            {
                return new ServiceResponse { Message = "Failed to send password reset email. Please try again later." };
            }
            
            return new ServiceResponse { Succeeded = true, Message = "If email exists, password reset link has been sent" };
        }

        // Reset password with token
        public async Task<ServiceResponse> ResetPassword(ResetPasswordRequest request)
        {
            logger.LogInformation($"Password reset attempt for email: {request.Email}");
            var validation = await validationService.ValidateAsync(request, resetPasswordValidator);
            if (!validation.Succeeded) return validation;

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                logger.LogWarning($"Password reset failed - user not found: {request.Email}");
                return new ServiceResponse { Message = "User not found" };
            }

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                logger.LogError(new Exception($"Password reset failed: {string.Join(";", result.Errors.Select(e => e.Description))}"), 
                    $"Failed to reset password for: {request.Email}");
                return new ServiceResponse
                {
                    Message = string.Join(";", result.Errors.Select(e => e.Description))
                };
            }

            // Revoke all refresh tokens for security
            await tokenManagement.RevokeAllUserRefreshTokens(user.Id);
            logger.LogInformation($"Password reset successful for email: {request.Email}, all refresh tokens revoked");
            return new ServiceResponse { Succeeded = true, Message = "Password reset successfully" };
        }

        // Send email confirmation
        public async Task<ServiceResponse> SendEmailConfirmation(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new ServiceResponse { Message = "User not found" };
            }

            if (user.EmailConfirmed)
            {
                return new ServiceResponse { Message = "Email is already confirmed" };
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var emailSent = await emailService.SendEmailConfirmationEmailAsync(
                user.Email!,
                token,
                user.FullName ?? user.Email!
            );

            if (!emailSent)
            {
                return new ServiceResponse { Message = "Failed to send confirmation email. Please try again later." };
            }

            return new ServiceResponse { Succeeded = true, Message = "Confirmation email sent" };
        }

        // Confirm email
        public async Task<ServiceResponse> ConfirmEmail(string email, string token)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new ServiceResponse { Message = "User not found" };
            }

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return new ServiceResponse
                {
                    Message = string.Join(";", result.Errors.Select(e => e.Description))
                };
            }

            return new ServiceResponse { Succeeded = true, Message = "Email confirmed successfully" };
        }

        // Update profile
        public async Task<ServiceResponse> UpdateProfile(string userId, UpdateProfileRequest request)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResponse { Message = "User not found" };
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return new ServiceResponse
                {
                    Message = string.Join(";", result.Errors.Select(e => e.Description))
                };
            }

            return new ServiceResponse { Succeeded = true, Message = "Profile updated successfully" };
        }
    }
}