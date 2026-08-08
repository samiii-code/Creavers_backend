namespace Creavers.API.Models
{
    /// <summary>System-generated audit log for important entity changes.</summary>
    public class AuditLog
    {
        public Guid     Id         { get; set; } = Guid.NewGuid();
        public Guid     UserId     { get; set; }
        public string   Action     { get; set; } = string.Empty;   // e.g. "BookingAccepted"
        public string   EntityName { get; set; } = string.Empty;   // e.g. "Booking"
        public string   EntityId   { get; set; } = string.Empty;   // stringified Guid
        public string?  Details    { get; set; }                   // optional JSON details
        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser User { get; set; } = null!;
    }
}
