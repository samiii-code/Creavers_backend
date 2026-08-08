using Creavers.API.DTOs.Jobs;

namespace Creavers.API.Interfaces
{
    public interface IJobService
    {
        /// <summary>Transition booking job status to EnRoute. Provider only.</summary>
        Task<JobStatusResponse> SetEnRouteAsync(Guid bookingId, Guid providerUserId, CancellationToken ct = default);

        /// <summary>Transition booking job status to Arrived. Provider only.</summary>
        Task<JobStatusResponse> SetArrivedAsync(Guid bookingId, Guid providerUserId, CancellationToken ct = default);

        /// <summary>Customer requests an OTP to start the job. Generates 6-digit OTP, stores in DB.</summary>
        Task<RequestStartOtpResponse> RequestStartOtpAsync(Guid bookingId, Guid customerUserId, CancellationToken ct = default);

        /// <summary>Provider submits OTP. If valid → status moves to InProgress.</summary>
        Task<JobStatusResponse> VerifyStartOtpAsync(Guid bookingId, Guid providerUserId, string otp, CancellationToken ct = default);

        /// <summary>Mark job as Completed. Provider or Customer (or Admin) can complete.</summary>
        Task<JobStatusResponse> CompleteJobAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default);

        /// <summary>Upload one piece of completion evidence (photo + description).</summary>
        Task<CompletionEvidenceResponse> UploadEvidenceAsync(Guid bookingId, Guid providerUserId, IFormFile photo, string? description, CancellationToken ct = default);

        /// <summary>Get the full chronological timeline for a booking.</summary>
        Task<IEnumerable<JobTimelineResponse>> GetTimelineAsync(Guid bookingId, Guid requestingUserId, CancellationToken ct = default);
    }
}
