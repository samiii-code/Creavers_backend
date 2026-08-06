using Creavers.API.Models.Enums;

namespace Creavers.API.DTOs.Auth
{
    /// <summary>Request to send an OTP to a user.</summary>
    public class SendOtpRequest
    {
        /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
        public Guid       UserId  { get; set; }

        /// <example>PhoneVerification</example>
        public OtpPurpose Purpose { get; set; }
    }

    /// <summary>Request to verify an OTP code.</summary>
    public class VerifyOtpRequest
    {
        /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
        public Guid       UserId  { get; set; }

        /// <example>482931</example>
        public string     Code    { get; set; } = string.Empty;

        /// <example>PhoneVerification</example>
        public OtpPurpose Purpose { get; set; }
    }

    /// <summary>Request to resend an OTP code.</summary>
    public class ResendOtpRequest
    {
        /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
        public Guid       UserId  { get; set; }

        /// <example>PhoneVerification</example>
        public OtpPurpose Purpose { get; set; }
    }

    /// <summary>Response after generating or verifying an OTP.</summary>
    public class OtpResponse
    {
        public string  Message { get; set; } = string.Empty;

        /// <summary>Returned only in development environment for testing purposes.</summary>
        public string? OtpCode { get; set; }
    }
}
