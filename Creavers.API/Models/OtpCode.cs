using Creavers.API.Models.Enums;

namespace Creavers.API.Models
{
    /// <summary>One-time password record used for phone/email verification and password reset.</summary>
    public class OtpCode : BaseEntity
    {
        public Guid       UserId    { get; set; }
        public string     Code      { get; set; } = string.Empty;
        public OtpPurpose Purpose   { get; set; }
        public DateTime   ExpiresAt { get; set; }
        public bool       IsUsed    { get; set; } = false;

        // Navigation
        public ApplicationUser User { get; set; } = null!;
    }
}
