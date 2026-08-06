namespace Creavers.API.Models
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;

        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }
}
