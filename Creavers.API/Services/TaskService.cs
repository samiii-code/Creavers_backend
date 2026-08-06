using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Data;
using Creavers.API.DTOs.Tasks;
using Creavers.API.Interfaces;
using Creavers.API.Models;
using Creavers.API.Models.Enums;

namespace Creavers.API.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper              _mapper;
        private readonly ILogger<TaskService> _logger;

        public TaskService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<TaskService> logger)
        {
            _context = context;
            _mapper  = mapper;
            _logger  = logger;
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<TaskResponse> CreateAsync(
            CreateTaskRequest request,
            Guid customerId,
            string? imagePath,
            CancellationToken cancellationToken = default)
        {
            // Ensure category exists
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Category '{request.CategoryId}' not found.");

            var task = _mapper.Map<CustomerTask>(request);
            task.Id         = Guid.NewGuid();
            task.CustomerId = customerId;
            task.Status     = CustomerTaskStatus.Pending;
            task.ImagePath  = imagePath;
            task.CreatedAt  = DateTime.UtcNow;

            await _context.CustomerTasks.AddAsync(task, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task created: {TaskId} by Customer {CustomerId}", task.Id, customerId);

            return await BuildResponseAsync(task.Id, cancellationToken)
                   ?? throw new InvalidOperationException("Task not found after creation.");
        }

        // ── GET ALL (Admin) ───────────────────────────────────────────────────
        public async Task<IEnumerable<TaskResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var tasks = await _context.CustomerTasks
                .Include(t => t.Customer)
                .Include(t => t.Category)
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<TaskResponse>>(tasks);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await BuildResponseAsync(id, cancellationToken);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<TaskResponse> UpdateAsync(
            Guid id,
            UpdateTaskRequest request,
            Guid requesterId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var task = await _context.CustomerTasks
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Task '{id}' not found.");

            if (!isAdmin && task.CustomerId != requesterId)
                throw new UnauthorizedAccessException("You are not authorised to update this task.");

            // Patch non-null fields
            if (request.Title         != null) task.Title         = request.Title;
            if (request.Description   != null) task.Description   = request.Description;
            if (request.CategoryId    != null) task.CategoryId    = request.CategoryId.Value;
            if (request.Address       != null) task.Address       = request.Address;
            if (request.SubCity       != null) task.SubCity       = request.SubCity;
            if (request.Woreda        != null) task.Woreda        = request.Woreda;
            if (request.Landmark      != null) task.Landmark      = request.Landmark;
            if (request.Budget        != null) task.Budget        = request.Budget.Value;
            if (request.PreferredDate != null) task.PreferredDate = request.PreferredDate.Value;
            if (request.Status        != null) task.Status        = request.Status.Value;

            task.UpdatedAt = DateTime.UtcNow;

            _context.CustomerTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task updated: {TaskId}", id);

            return await BuildResponseAsync(task.Id, cancellationToken)
                   ?? throw new InvalidOperationException("Task not found after update.");
        }

        // ── DELETE (soft) ─────────────────────────────────────────────────────
        public async Task DeleteAsync(
            Guid id,
            Guid requesterId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var task = await _context.CustomerTasks
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Task '{id}' not found.");

            if (!isAdmin && task.CustomerId != requesterId)
                throw new UnauthorizedAccessException("You are not authorised to delete this task.");

            task.IsDeleted  = true;
            task.UpdatedAt  = DateTime.UtcNow;

            _context.CustomerTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task deleted (soft): {TaskId}", id);
        }

        // ── MY TASKS (Customer) ───────────────────────────────────────────────
        public async Task<IEnumerable<TaskResponse>> GetMyTasksAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _context.CustomerTasks
                .Include(t => t.Customer)
                .Include(t => t.Category)
                .Where(t => t.CustomerId == customerId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<TaskResponse>>(tasks);
        }

        // ── RECOMMENDED PROVIDERS (Matching) ──────────────────────────────────
        public async Task<IEnumerable<RecommendedProviderDto>> GetRecommendedProvidersAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var task = await _context.CustomerTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Task '{taskId}' not found.");

            // Matching criteria:
            // 1. Same Category
            // 2. Provider Status = Approved
            var providers = await _context.ProviderProfiles
                .Include(p => p.ApplicationUser)
                .Include(p => p.Category)
                .Where(p => p.CategoryId == task.CategoryId && p.Status == ProviderStatus.Approved && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            var recommendations = providers.Select(p =>
            {
                var isSubCityMatch = !string.IsNullOrWhiteSpace(p.ServiceArea) &&
                                     !string.IsNullOrWhiteSpace(task.SubCity) &&
                                     (p.ServiceArea.Contains(task.SubCity, StringComparison.OrdinalIgnoreCase) ||
                                      task.SubCity.Contains(p.ServiceArea, StringComparison.OrdinalIgnoreCase));

                var distance = CalculateDistance(p.Latitude, p.Longitude, task.Latitude, task.Longitude);
                var providerDto = _mapper.Map<Creavers.API.DTOs.Providers.ProviderProfileDto>(p);

                return new
                {
                    Dto = new RecommendedProviderDto
                    {
                        Provider = providerDto,
                        Distance = distance,
                        Experience = p.ExperienceYears,
                        Rating = 5.0 // Rating placeholder
                    },
                    IsSubCityMatch = isSubCityMatch,
                    Distance = distance,
                    Experience = p.ExperienceYears
                };
            });

            var sorted = recommendations
                .OrderByDescending(r => r.IsSubCityMatch)
                .ThenBy(r => r.Distance.HasValue ? 0 : 1)
                .ThenBy(r => r.Distance)
                .ThenByDescending(r => r.Experience)
                .Select(r => r.Dto)
                .ToList();

            return sorted;
        }

        private static double? CalculateDistance(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
                return null;

            var R = 6371.0;
            var dLat = ToRadians(lat2.Value - lat1.Value);
            var dLon = ToRadians(lon2.Value - lon1.Value);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1.Value)) * Math.Cos(ToRadians(lat2.Value)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(R * c, 2);
        }

        private static double ToRadians(double val) => Math.PI * val / 180.0;

        // ── HELPER ────────────────────────────────────────────────────────────
        private async Task<TaskResponse?> BuildResponseAsync(Guid id, CancellationToken cancellationToken)
        {
            var task = await _context.CustomerTasks
                .Include(t => t.Customer)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

            return task == null ? null : _mapper.Map<TaskResponse>(task);
        }
    }
}
