using System.Text.Json.Serialization;

namespace Creavers.API.DTOs
{
    public class HealthCheckResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("message")]
        public string Message { get; set; } = "Creavers API is running.";
    }
}
