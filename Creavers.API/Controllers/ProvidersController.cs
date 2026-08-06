using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Bookings;
using Creavers.API.DTOs.Providers;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Provider profile management endpoints.</summary>
    [ApiController]
    [Route("api/providers")]
    [Authorize]
    [Produces("application/json")]
    public class ProvidersController : ControllerBase
    {
        private readonly IProviderService _providerService;
        private readonly IBookingService  _bookingService;
        private readonly IValidator<CreateProviderProfileRequest> _createValidator;
        private readonly IValidator<UpdateProviderProfileRequest> _updateValidator;

        public ProvidersController(
            IProviderService providerService,
            IBookingService  bookingService,
            IValidator<CreateProviderProfileRequest> createValidator,
            IValidator<UpdateProviderProfileRequest> updateValidator)
        {
            _providerService  = providerService;
            _bookingService   = bookingService;
            _createValidator  = createValidator;
            _updateValidator  = updateValidator;
        }

        /// <summary>Create a provider profile. PROVIDER role only. One profile per user.</summary>
        [HttpPost("profile")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<ProviderProfileDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateProfile([FromBody] CreateProviderProfileRequest request, CancellationToken cancellationToken)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            var userId = GetCurrentUserId();

            try
            {
                var profile = await _providerService.CreateProfileAsync(userId, request, cancellationToken);
                return StatusCode(StatusCodes.Status201Created,
                    ApiResponse<ProviderProfileDto>.SuccessResult(profile, "Provider profile created successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Get all provider profiles. Requires authentication.</summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProviderProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllProfiles(CancellationToken cancellationToken)
        {
            var profiles = await _providerService.GetAllProfilesAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<ProviderProfileDto>>.SuccessResult(profiles));
        }

        /// <summary>Get a provider profile by ID.</summary>
        [HttpGet("profile/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProviderProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfileById(Guid id, CancellationToken cancellationToken)
        {
            var profile = await _providerService.GetProfileByIdAsync(id, cancellationToken);
            if (profile == null)
                return NotFound(ApiResponse<object>.FailureResult($"Provider profile '{id}' not found."));

            return Ok(ApiResponse<ProviderProfileDto>.SuccessResult(profile));
        }

        /// <summary>Update the authenticated provider's profile. PROVIDER only.</summary>
        [HttpPut("profile")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<ProviderProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProviderProfileRequest request, CancellationToken cancellationToken)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            var userId = GetCurrentUserId();

            try
            {
                var profile = await _providerService.UpdateProfileAsync(userId, request, cancellationToken);
                if (profile == null)
                    return NotFound(ApiResponse<object>.FailureResult("Provider profile not found."));

                return Ok(ApiResponse<ProviderProfileDto>.SuccessResult(profile, "Profile updated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────

        /// <summary>Get the booking history for the authenticated provider. PROVIDER role only.</summary>
        [HttpGet("history")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookingResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProviderHistory(CancellationToken cancellationToken)
        {
            var providerUserId = GetCurrentUserId();
            try
            {
                var history = await _bookingService.GetProviderHistoryAsync(providerUserId, cancellationToken);
                return Ok(ApiResponse<IEnumerable<BookingResponse>>.SuccessResult(history));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        // ─── Private Helpers ────────────────────────────────────────────────
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException("User ID claim missing.");
            return Guid.Parse(claim);
        }
    }
}
