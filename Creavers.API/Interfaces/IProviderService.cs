using Creavers.API.DTOs.Providers;

namespace Creavers.API.Interfaces
{
    public interface IProviderService
    {
        Task<ProviderProfileDto> CreateProfileAsync(Guid userId, CreateProviderProfileRequest request, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProviderProfileDto>> GetAllProfilesAsync(CancellationToken cancellationToken = default);
        Task<ProviderProfileDto?> GetProfileByIdAsync(Guid profileId, CancellationToken cancellationToken = default);
        Task<ProviderProfileDto?> UpdateProfileAsync(Guid userId, UpdateProviderProfileRequest request, CancellationToken cancellationToken = default);
    }
}
