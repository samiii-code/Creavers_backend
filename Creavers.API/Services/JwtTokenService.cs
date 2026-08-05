using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Creavers.API.Configurations;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public (string token, DateTime expiresAt) GenerateToken(ApplicationUser user, string role)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString()),
                new Claim(ClaimTypes.Email,              user.Email ?? string.Empty),
                new Claim(ClaimTypes.MobilePhone,        user.PhoneNumber ?? string.Empty),
                new Claim(ClaimTypes.Role,               role),
                new Claim("FullName",                    user.FullName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
