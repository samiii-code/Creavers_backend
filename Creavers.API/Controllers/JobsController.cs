using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Jobs;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Job lifecycle management — status transitions, OTP, evidence, and timeline.</summary>
    [ApiController]
    [Route("api/jobs")]
    [Authorize]
    [Produces("application/json")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IValidator<VerifyStartOtpRequest> _otpValidator;
        private readonly ILogger<JobsController> _logger;

        public JobsController(
            IJobService jobService,
            IValidator<VerifyStartOtpRequest> otpValidator,
            ILogger<JobsController> logger)
        {
            _jobService   = jobService;
            _otpValidator = otpValidator;
            _logger       = logger;
        }

        // ─── PATCH /api/jobs/{bookingId}/enroute ────────────────────────────────
        /// <summary>Provider marks themselves as en route to the customer. PROVIDER role only.</summary>
        /// <param name="bookingId">The booking identifier.</param>
        [HttpPatch("{bookingId:guid}/enroute")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetEnRoute(Guid bookingId, CancellationToken ct)
        {
            try
            {
                var result = await _jobService.SetEnRouteAsync(bookingId, GetCurrentUserId(), ct);
                return Ok(ApiResponse<JobStatusResponse>.SuccessResult(result));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── PATCH /api/jobs/{bookingId}/arrived ────────────────────────────────
        /// <summary>Provider marks themselves as arrived at the job site. PROVIDER role only.</summary>
        [HttpPatch("{bookingId:guid}/arrived")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetArrived(Guid bookingId, CancellationToken ct)
        {
            try
            {
                var result = await _jobService.SetArrivedAsync(bookingId, GetCurrentUserId(), ct);
                return Ok(ApiResponse<JobStatusResponse>.SuccessResult(result));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── POST /api/jobs/{bookingId}/request-start-otp ──────────────────────
        /// <summary>Customer requests a 6-digit OTP to hand to the provider. CUSTOMER role only.</summary>
        [HttpPost("{bookingId:guid}/request-start-otp")]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(ApiResponse<RequestStartOtpResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestStartOtp(Guid bookingId, CancellationToken ct)
        {
            try
            {
                var result = await _jobService.RequestStartOtpAsync(bookingId, GetCurrentUserId(), ct);
                return Ok(ApiResponse<RequestStartOtpResponse>.SuccessResult(result));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── POST /api/jobs/{bookingId}/verify-start-otp ───────────────────────
        /// <summary>Provider submits the OTP received from the customer to start the job. PROVIDER role only.</summary>
        [HttpPost("{bookingId:guid}/verify-start-otp")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyStartOtp(
            Guid bookingId, [FromBody] VerifyStartOtpRequest request, CancellationToken ct)
        {
            var validation = await _otpValidator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(ApiResponse<object>.FailureResult(
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
            try
            {
                var result = await _jobService.VerifyStartOtpAsync(bookingId, GetCurrentUserId(), request.Otp, ct);
                return Ok(ApiResponse<JobStatusResponse>.SuccessResult(result));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── PATCH /api/jobs/{bookingId}/complete ──────────────────────────────
        /// <summary>Mark the job as completed. Allowed by the Provider, Customer, or Admin.</summary>
        [HttpPatch("{bookingId:guid}/complete")]
        [Authorize(Roles = "PROVIDER,CUSTOMER,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteJob(Guid bookingId, CancellationToken ct)
        {
            try
            {
                var isAdmin = User.IsInRole("ADMIN");
                var result = await _jobService.CompleteJobAsync(bookingId, GetCurrentUserId(), isAdmin, ct);
                return Ok(ApiResponse<JobStatusResponse>.SuccessResult(result));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── POST /api/jobs/{bookingId}/completion-evidence ────────────────────
        /// <summary>Provider uploads a photo as completion evidence. Supports multipart/form-data. PROVIDER role only.</summary>
        [HttpPost("{bookingId:guid}/completion-evidence")]
        [Authorize(Roles = "PROVIDER")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<CompletionEvidenceResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadCompletionEvidence(
            Guid bookingId,
            IFormFile photo,
            [FromForm] string? description,
            CancellationToken ct)
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(ApiResponse<object>.FailureResult("Photo file is required."));
            try
            {
                var result = await _jobService.UploadEvidenceAsync(
                    bookingId, GetCurrentUserId(), photo, description, ct);
                return StatusCode(StatusCodes.Status201Created,
                    ApiResponse<CompletionEvidenceResponse>.SuccessResult(result, "Evidence uploaded successfully."));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── GET /api/jobs/{bookingId}/timeline ────────────────────────────────
        /// <summary>Get the full job timeline for a booking. Participants only.</summary>
        [HttpGet("{bookingId:guid}/timeline")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<JobTimelineResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTimeline(Guid bookingId, CancellationToken ct)
        {
            try
            {
                var result = await _jobService.GetTimelineAsync(bookingId, GetCurrentUserId(), ct);
                return Ok(ApiResponse<IEnumerable<JobTimelineResponse>>.SuccessResult(result));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException("User ID claim missing.");
            return Guid.Parse(claim);
        }
    }
}
