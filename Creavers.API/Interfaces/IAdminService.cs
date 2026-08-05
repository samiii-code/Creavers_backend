using Creavers.API.DTOs.Providers;

namespace Creavers.API.Interfaces
{
    public interface IAdminService
    {
        Task<IEnumerable<ProviderProfileDto>> GetAllProvidersAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<ProviderProfileDto>> GetPendingProvidersAsync(CancellationToken cancellationToken = default);
        Task<ProviderProfileDto?> ApproveProviderAsync(Guid profileId, CancellationToken cancellationToken = default);
        Task<ProviderProfileDto?> RejectProviderAsync(Guid profileId, CancellationToken cancellationToken = default);
    }
}
