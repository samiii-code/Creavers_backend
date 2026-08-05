using Creavers.API.Models.Enums;

namespace Creavers.API.DTOs.Providers
{
    public class ProviderProfileDto
    {
        public Guid Id { get; set; }
        public Guid ApplicationUserId { get; set; }
        public string ProviderFullName { get; set; } = string.Empty;
        public string ProviderEmail { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string ServiceArea { get; set; } = string.Empty;
        public string Availability { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string? LicenseDocument { get; set; }
        public ProviderStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
