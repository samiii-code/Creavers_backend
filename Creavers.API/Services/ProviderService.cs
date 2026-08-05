using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Data;
using Creavers.API.DTOs.Providers;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class ProviderService : IProviderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProviderService> _logger;

        public ProviderService(ApplicationDbContext context, IMapper mapper, ILogger<ProviderService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProviderProfileDto> CreateProfileAsync(Guid userId, CreateProviderProfileRequest request, CancellationToken cancellationToken = default)
        {
            // Enforce one profile per user
            var exists = await _context.ProviderProfiles
                .AnyAsync(p => p.ApplicationUserId == userId && !p.IsDeleted, cancellationToken);

            if (exists)
                throw new InvalidOperationException("You already have a provider profile.");

            // Validate category exists
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

            if (!categoryExists)
                throw new KeyNotFoundException("Category not found.");

            var profile = _mapper.Map<ProviderProfile>(request);
            profile.ApplicationUserId = userId;

            await _context.ProviderProfiles.AddAsync(profile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Provider profile created for user {UserId}", userId);

            // Reload with navigation properties
            return await LoadProfileDtoAsync(profile.Id, cancellationToken)
                   ?? throw new InvalidOperationException("Failed to load created profile.");
        }

        public async Task<IEnumerable<ProviderProfileDto>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
        {
            var profiles = await _context.ProviderProfiles
                .Include(p => p.ApplicationUser)
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<ProviderProfileDto>>(profiles);
        }

        public async Task<ProviderProfileDto?> GetProfileByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
        {
            return await LoadProfileDtoAsync(profileId, cancellationToken);
        }

        public async Task<ProviderProfileDto?> UpdateProfileAsync(Guid userId, UpdateProviderProfileRequest request, CancellationToken cancellationToken = default)
        {
            var profile = await _context.ProviderProfiles
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId && !p.IsDeleted, cancellationToken);

            if (profile == null)
                throw new KeyNotFoundException("Provider profile not found.");

            // Validate category
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

            if (!categoryExists)
                throw new KeyNotFoundException("Category not found.");

            _mapper.Map(request, profile);
            profile.UpdatedAt = DateTime.UtcNow;

            _context.ProviderProfiles.Update(profile);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Provider profile updated for user {UserId}", userId);

            return await LoadProfileDtoAsync(profile.Id, cancellationToken);
        }

        // ─── private helpers ────────────────────────────────────────────────
        private async Task<ProviderProfileDto?> LoadProfileDtoAsync(Guid profileId, CancellationToken cancellationToken)
        {
            var profile = await _context.ProviderProfiles
                .Include(p => p.ApplicationUser)
                .Include(p => p.Category)
                .Where(p => p.Id == profileId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return profile == null ? null : _mapper.Map<ProviderProfileDto>(profile);
        }
    }
}
