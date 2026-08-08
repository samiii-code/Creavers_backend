namespace Creavers.API.Models
{
    /// <summary>In-app chat message scoped to a single booking.</summary>
    public class ChatMessage : BaseEntity
    {
        public Guid   BookingId { get; set; }
        public Guid   SenderId  { get; set; }
        public string Message   { get; set; } = string.Empty;
        public DateTime SentAt  { get; set; } = DateTime.UtcNow;
        public bool   IsRead    { get; set; } = false;

        // Navigation
        public Booking        Booking { get; set; } = null!;
        public ApplicationUser Sender  { get; set; } = null!;
    }
}
