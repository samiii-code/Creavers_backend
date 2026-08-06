using Creavers.API.Models.Enums;

namespace Creavers.API.Models
{
    public class Booking : BaseEntity
    {
        public Guid TaskId { get; set; }
        public Guid ProviderId { get; set; }
        public Guid CustomerId { get; set; }
        public BookingStatus BookingStatus { get; set; } = BookingStatus.Pending;
        public string? Notes { get; set; }
        public DateTime? ScheduledDate { get; set; }

        // Navigation properties
        public CustomerTask Task { get; set; } = null!;
        public ProviderProfile Provider { get; set; } = null!;
        public ApplicationUser Customer { get; set; } = null!;
    }
}
