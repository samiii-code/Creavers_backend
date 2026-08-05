using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
        {
            _healthService = healthService;
        }

        [HttpGet]
        public ActionResult<HealthCheckResponse> Get()
        {
            var response = _healthService.GetHealthStatus();
            return Ok(response);
        }
    }
}
