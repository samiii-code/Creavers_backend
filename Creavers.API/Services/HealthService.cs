using Creavers.API.DTOs;
using Creavers.API.Interfaces;

namespace Creavers.API.Services
{
    public class HealthService : IHealthService
    {
        public HealthCheckResponse GetHealthStatus()
        {
            return new HealthCheckResponse
            {
                Success = true,
                Message = "Creavers API is running."
            };
        }
    }
}
