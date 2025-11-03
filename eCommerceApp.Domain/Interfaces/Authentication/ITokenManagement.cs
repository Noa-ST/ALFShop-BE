using System.Security.Claims;

namespace eCommerceApp.Domain.Interfaces.Authentication
{
    public interface ITokenManagement
    {
        string GetRefreshToken();
        List<Claim> GetUserClaimsFromToken(string token);
        Task<bool> ValidateRefreshToken(string refreshToken);
        Task<string> GetUserIdByRefreshToken(string refreshToken);
        Task<int> AddRefreshToken(string userId, string refreshToken);
        Task<int> UpdateRefreshToken(string userId, string refreshToken);
        Task<string> UpdateRefreshTokenAndGetNew(string userId, string oldRefreshToken);
        Task<int> RevokeRefreshToken(string refreshToken);
        Task<int> RevokeAllUserRefreshTokens(string userId);
        Task<int> CleanupExpiredTokens();
        string GenerateToken(List<Claim> claims);
    }
}
