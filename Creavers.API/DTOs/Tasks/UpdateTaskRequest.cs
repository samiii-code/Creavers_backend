using Creavers.API.Models.Enums;

namespace Creavers.API.DTOs.Tasks
{
    /// <summary>Request body to update an existing customer task.</summary>
    public class UpdateTaskRequest
    {
        /// <example>Fix kitchen sink (updated)</example>
        public string? Title         { get; set; }

        /// <example>Updated description of the plumbing issue.</example>
        public string? Description   { get; set; }

        /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
        public Guid?   CategoryId    { get; set; }

        // ── Address ──────────────────────────────────────────────────────────

        /// <example>Bole Road, near Edna Mall</example>
        public string? Address       { get; set; }

        /// <example>Bole</example>
        public string? SubCity       { get; set; }

        /// <example>04</example>
        public string? Woreda        { get; set; }

        /// <example>Near Edna Mall</example>
        public string? Landmark      { get; set; }

        /// <example>8.9956</example>
        public double? Latitude      { get; set; }

        /// <example>38.7636</example>
        public double? Longitude     { get; set; }

        // ── Task details ──────────────────────────────────────────────────────

        /// <example>650.00</example>
        public decimal? Budget        { get; set; }

        /// <example>2026-08-12T09:00:00Z</example>
        public DateTime? PreferredDate { get; set; }

        /// <example>Pending</example>
        public CustomerTaskStatus? Status { get; set; }
    }
}
