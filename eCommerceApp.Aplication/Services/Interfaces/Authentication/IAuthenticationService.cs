using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Identity;

namespace eCommerceApp.Aplication.Services.Interfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse> CreateUser(CreateUser user);
        Task<LoginResponse> LoginUser(LoginUser user);
        Task<LoginResponse> ReviveToken(string refreshToken);
        Task<ServiceResponse> Logout(string userId, string refreshToken);
        Task<UserInfoDto?> GetCurrentUser(string userId);
        Task<ServiceResponse> ChangePassword(string userId, ChangePasswordRequest request);
        Task<ServiceResponse> ForgotPassword(string email);
        Task<ServiceResponse> ResetPassword(ResetPasswordRequest request);
        Task<ServiceResponse> SendEmailConfirmation(string email);
        Task<ServiceResponse> ConfirmEmail(string email, string token);
        Task<ServiceResponse> UpdateProfile(string userId, UpdateProfileRequest request);
    }
}
