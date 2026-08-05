using Creavers.API.DTOs.Auth;

namespace Creavers.API.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }
}
