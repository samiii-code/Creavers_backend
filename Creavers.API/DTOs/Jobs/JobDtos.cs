using Creavers.API.Models.Enums;

namespace Creavers.API.DTOs.Jobs
{
    /// <summary>Response returned after every job status transition.</summary>
    public class JobStatusResponse
    {
        public Guid      BookingId     { get; set; }
        public JobStatus JobStatus     { get; set; }
        public string    JobStatusName => JobStatus.ToString();
        public DateTime  UpdatedAt     { get; set; }
        public string?   Message       { get; set; }
    }

    /// <summary>Response for Request-Start-OTP. Contains OTP only in development mode.</summary>
    public class RequestStartOtpResponse
    {
        public Guid     BookingId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string?  Otp       { get; set; }   // only populated in Development
        public string   Message   { get; set; } = "OTP sent to customer.";
    }

    /// <summary>Provider submits this to verify the OTP and move job to InProgress.</summary>
    public class VerifyStartOtpRequest
    {
        /// <example>123456</example>
        public string Otp { get; set; } = string.Empty;
    }

    /// <summary>Response for multipart evidence upload.</summary>
    public class CompletionEvidenceResponse
    {
        public Guid     Id          { get; set; }
        public Guid     BookingId   { get; set; }
        public string   PhotoPath   { get; set; } = string.Empty;
        public string?  Description { get; set; }
        public DateTime UploadedAt  { get; set; }
    }

    /// <summary>Single entry in the job timeline.</summary>
    public class JobTimelineResponse
    {
        public Guid     Id            { get; set; }
        public Guid     BookingId     { get; set; }
        public string   Status        { get; set; } = string.Empty;
        public Guid     ChangedBy     { get; set; }
        public string   ChangedByName { get; set; } = string.Empty;
        public string?  Notes         { get; set; }
        public DateTime CreatedAt     { get; set; }
    }
}
