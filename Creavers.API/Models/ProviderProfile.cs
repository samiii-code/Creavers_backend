using Creavers.API.Models.Enums;

namespace Creavers.API.Models
{
    public class ProviderProfile : BaseEntity
    {
        public Guid ApplicationUserId { get; set; }
        public Guid CategoryId { get; set; }
        public int ExperienceYears { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string ServiceArea { get; set; } = string.Empty;
        public string Availability { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string? LicenseDocument { get; set; }
        public ProviderStatus Status { get; set; } = ProviderStatus.Pending;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Navigation
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
