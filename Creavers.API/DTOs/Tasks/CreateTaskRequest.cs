using Creavers.API.Models.Enums;

namespace Creavers.API.DTOs.Tasks
{
    /// <summary>Request body to create a new customer task.</summary>
    public class CreateTaskRequest
    {
        /// <example>Fix kitchen sink</example>
        public string  Title         { get; set; } = string.Empty;

        /// <example>The kitchen sink is leaking and needs a plumber urgently.</example>
        public string  Description   { get; set; } = string.Empty;

        /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
        public Guid    CategoryId    { get; set; }

        // ── Address ──────────────────────────────────────────────────────────

        /// <example>Bole Road, near Atlas Hotel</example>
        public string  Address       { get; set; } = string.Empty;

        /// <example>Bole</example>
        public string  SubCity       { get; set; } = string.Empty;

        /// <example>03</example>
        public string  Woreda        { get; set; } = string.Empty;

        /// <example>Near Atlas Hotel</example>
        public string? Landmark      { get; set; }

        // ── Task details ──────────────────────────────────────────────────────

        /// <example>500.00</example>
        public decimal Budget        { get; set; }

        /// <example>2026-08-10T09:00:00Z</example>
        public DateTime PreferredDate { get; set; }
    }
}
