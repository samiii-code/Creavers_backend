namespace Creavers.API.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(Guid userId, string action, string entityName, string entityId, string? details = null, CancellationToken ct = default);
    }
}
