using Creavers.API.DTOs;

namespace Creavers.API.Interfaces
{
    public interface IHealthService
    {
        HealthCheckResponse GetHealthStatus();
    }
}
