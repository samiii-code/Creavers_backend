namespace Creavers.API.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigation
        public ICollection<ProviderProfile> ProviderProfiles { get; set; } = new List<ProviderProfile>();
    }
}
