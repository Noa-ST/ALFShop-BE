using eCommerceApp.Domain.Entities.Identity;
using eCommerceApp.Domain.Interfaces.Authentication;
using eCommerceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace eCommerceApp.Infrastructure.Repositories.Authentication
{
    public class TokenManagement(AppDbContext context, IConfiguration config) : ITokenManagement
    {
        public async Task<int> AddRefreshToken(string userId, string refreshToken)
        {
            // Refresh token expires in 7 days
            var expiresAt = DateTime.UtcNow.AddDays(7);
            
            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsRevoked = false
            });
            return await context.SaveChangesAsync();
        }

        public string GenerateToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddHours(2);
            var token = new JwtSecurityToken(
                 issuer: config["JWT:Issuer"],
                 audience: config["JWT:Audience"],
                 claims: claims,
                 expires: expiration,
                 signingCredentials: cred);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GetRefreshToken()
        {
            const int byteSize = 64;
            byte[] randomBytes = new byte[byteSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            string token = Convert.ToBase64String(randomBytes);
            return WebUtility.UrlEncode(token);
        }

        public List<Claim> GetUserClaimsFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            if (jwtToken != null)
                return jwtToken.Claims.ToList();
            else
                return [];
        }

        public async Task<string> GetUserIdByRefreshToken(string refreshToken)
        {
            var token = await context.RefreshTokens.FirstOrDefaultAsync(_ => _.Token == refreshToken);
            return token?.UserId ?? string.Empty;
        }

        public async Task<int> UpdateRefreshToken(string userId, string refreshToken)
        {
            var user = await context.RefreshTokens.FirstOrDefaultAsync(_ => _.Token == refreshToken);
            if (user == null) return -1;
            user.Token = refreshToken;
            return await context.SaveChangesAsync();
        }

        public async Task<string> UpdateRefreshTokenAndGetNew(string userId, string oldRefreshToken)
        {
            // Revoke old token
            var oldToken = await context.RefreshTokens.FirstOrDefaultAsync(_ => _.Token == oldRefreshToken);
            if (oldToken != null)
            {
                oldToken.IsRevoked = true;
            }

            // Create new refresh token
            string newRefreshToken = GetRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);
            
            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                Token = newRefreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsRevoked = false
            });
            
            await context.SaveChangesAsync();
            return newRefreshToken;
        }

        public async Task<bool> ValidateRefreshToken(string refreshToken)
        {
            var token = await context.RefreshTokens.FirstOrDefaultAsync(_ => _.Token == refreshToken);
            if (token == null) return false;
            
            // Check if token is revoked or expired
            if (token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
                return false;
            
            return true;
        }

        public async Task<int> RevokeRefreshToken(string refreshToken)
        {
            var token = await context.RefreshTokens.FirstOrDefaultAsync(_ => _.Token == refreshToken);
            if (token == null) return 0;
            
            token.IsRevoked = true;
            return await context.SaveChangesAsync();
        }

        public async Task<int> RevokeAllUserRefreshTokens(string userId)
        {
            var tokens = await context.RefreshTokens
                .Where(_ => _.UserId == userId && !_.IsRevoked)
                .ToListAsync();
            
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }
            
            return await context.SaveChangesAsync();
        }
    }
}
