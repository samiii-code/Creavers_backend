namespace Creavers.API.DTOs.Providers
{
    public class CreateProviderProfileRequest
    {
        public Guid CategoryId { get; set; }
        public int ExperienceYears { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string ServiceArea { get; set; } = string.Empty;
        public string Availability { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string? LicenseDocument { get; set; }
    }
}
