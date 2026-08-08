using Creavers.API.Data;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger  = logger;
        }

        public async Task LogAsync(
            Guid userId,
            string action,
            string entityName,
            string entityId,
            string? details = null,
            CancellationToken ct = default)
        {
            var log = new AuditLog
            {
                UserId     = userId,
                Action     = action,
                EntityName = entityName,
                EntityId   = entityId,
                Details    = details,
                CreatedAt  = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "AuditLog: Action={Action} Entity={EntityName}({EntityId}) By={UserId}",
                action, entityName, entityId, userId);
        }
    }
}
