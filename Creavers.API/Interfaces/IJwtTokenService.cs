using Creavers.API.Models;

namespace Creavers.API.Interfaces
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAt) GenerateToken(ApplicationUser user, string role);
    }
}
