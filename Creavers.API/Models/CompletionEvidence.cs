namespace Creavers.API.Models
{
    /// <summary>Photo/description evidence uploaded by the provider when completing a job.</summary>
    public class CompletionEvidence : BaseEntity
    {
        public Guid    BookingId   { get; set; }
        public string  PhotoPath   { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public Guid    UploadedBy  { get; set; }  // UserId of uploader

        // Navigation
        public Booking Booking { get; set; } = null!;
    }
}
