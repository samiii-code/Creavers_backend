namespace Creavers.API.DTOs.Bookings
{
    public class CreateBookingRequest
    {
        public Guid TaskId { get; set; }
        public Guid ProviderId { get; set; }
        public string? Notes { get; set; }
        public DateTime? ScheduledDate { get; set; }
    }
}
