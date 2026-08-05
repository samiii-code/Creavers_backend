using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Data;
using Creavers.API.DTOs.Providers;
using Creavers.API.Interfaces;
using Creavers.API.Models.Enums;

namespace Creavers.API.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminService> _logger;

        public AdminService(ApplicationDbContext context, IMapper mapper, ILogger<AdminService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ProviderProfileDto>> GetAllProvidersAsync(CancellationToken cancellationToken = default)
        {
            var profiles = await _context.ProviderProfiles
                .Include(p => p.ApplicationUser)
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<ProviderProfileDto>>(profiles);
        }

        public async Task<IEnumerable<ProviderProfileDto>> GetPendingProvidersAsync(CancellationToken cancellationToken = default)
        {
            var profiles = await _context.ProviderProfiles
                .Include(p => p.ApplicationUser)
                .Include(p => p.Category)
                .Where(p => p.Status == ProviderStatus.Pending && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<ProviderProfileDto>>(profiles);
        }

        public async Task<ProviderProfileDto?> ApproveProviderAsync(Guid profileId, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(profileId, ProviderStatus.Approved, cancellationToken);
        }

        public async Task<ProviderProfileDto?> RejectProviderAsync(Guid profileId, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(profileId, ProviderStatus.Rejected, cancellationToken);
        }

        // ─── private helpers ────────────────────────────────────────────────
        private async Task<ProviderProfileDto?> ChangeStatusAsync(Guid profileId, ProviderStatus newStatus, CancellationToken cancellationToken)
        {
            var profile = await _context.ProviderProfiles
                .Include(p => p.ApplicationUser)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == profileId && !p.IsDeleted, cancellationToken);

            if (profile == null) return null;

            profile.Status = newStatus;
            profile.UpdatedAt = DateTime.UtcNow;

            _context.ProviderProfiles.Update(profile);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Provider profile {ProfileId} status changed to {Status}", profileId, newStatus);

            return _mapper.Map<ProviderProfileDto>(profile);
        }
    }
}
