using Creavers.API.Models.Enums;

namespace Creavers.API.DTOs.Bookings
{
    public class BookingResponse
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public BookingStatus BookingStatus { get; set; }
        public string StatusName => BookingStatus.ToString();
        public string? Notes { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
