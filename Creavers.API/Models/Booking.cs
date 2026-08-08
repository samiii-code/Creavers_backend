using Creavers.API.Models.Enums;

namespace Creavers.API.Models
{
    public class Booking : BaseEntity
    {
        public Guid   TaskId        { get; set; }
        public Guid   ProviderId    { get; set; }
        public Guid   CustomerId    { get; set; }
        public BookingStatus BookingStatus { get; set; } = BookingStatus.Pending;
        public JobStatus     JobStatus     { get; set; } = JobStatus.Accepted;
        public string? Notes         { get; set; }
        public DateTime? ScheduledDate { get; set; }

        // Navigation properties
        public CustomerTask   Task      { get; set; } = null!;
        public ProviderProfile Provider  { get; set; } = null!;
        public ApplicationUser Customer  { get; set; } = null!;

        // Week 5 navigations
        public ICollection<CompletionEvidence> CompletionEvidences { get; set; } = new List<CompletionEvidence>();
        public ICollection<JobTimeline>        Timelines           { get; set; } = new List<JobTimeline>();
        public ICollection<ChatMessage>        ChatMessages        { get; set; } = new List<ChatMessage>();
    }
}
