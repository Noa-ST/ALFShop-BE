using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Identity;

namespace eCommerceApp.Aplication.Services.Interfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse> CreateUser(CreateUser user);

        Task<LoginResponse> LoginUser(LoginUser user);

        Task<LoginResponse> ReviveToken(string refreshToken);
    }
}
