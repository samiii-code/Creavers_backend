using Creavers.API.DTOs.Auth;

namespace Creavers.API.Interfaces
{
    public interface IOtpService
    {
        Task<OtpResponse> SendOtpAsync(SendOtpRequest request, bool isDevelopment, CancellationToken cancellationToken = default);
        Task<OtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
        Task<OtpResponse> ResendOtpAsync(ResendOtpRequest request, bool isDevelopment, CancellationToken cancellationToken = default);
    }
}
