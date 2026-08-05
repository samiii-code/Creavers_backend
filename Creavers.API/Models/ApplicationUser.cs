using Microsoft.AspNetCore.Identity;

namespace Creavers.API.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ProviderProfile? ProviderProfile { get; set; }
    }
}
