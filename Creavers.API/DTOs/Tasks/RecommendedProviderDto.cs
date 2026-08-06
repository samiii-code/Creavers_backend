using Creavers.API.DTOs.Providers;

namespace Creavers.API.DTOs.Tasks
{
    public class RecommendedProviderDto
    {
        public ProviderProfileDto Provider { get; set; } = null!;
        public double? Distance { get; set; }
        public int Experience { get; set; }
        public double Rating { get; set; } = 5.0;
    }
}
