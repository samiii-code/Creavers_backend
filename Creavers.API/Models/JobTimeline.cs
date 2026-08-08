using Creavers.API.Models.Enums;

namespace Creavers.API.Models
{
    /// <summary>Immutable audit trail entry for every job status transition.</summary>
    public class JobTimeline : BaseEntity
    {
        public Guid      BookingId  { get; set; }
        public JobStatus Status     { get; set; }
        public Guid      ChangedBy  { get; set; }   // UserId who triggered the change
        public string?   Notes      { get; set; }

        // Navigation
        public Booking        Booking   { get; set; } = null!;
        public ApplicationUser ChangedByUser { get; set; } = null!;
    }
}
