using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Bookings;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Booking management endpoints for customers and providers.</summary>
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    [Produces("application/json")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IValidator<CreateBookingRequest> _createValidator;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(
            IBookingService bookingService,
            IValidator<CreateBookingRequest> createValidator,
            ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _createValidator = createValidator;
            _logger = logger;
        }

        /// <summary>Create a new booking request. CUSTOMER role only.</summary>
        [HttpPost]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(ApiResponse<BookingResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResult("Validation failed.", errors));
            }

            var customerId = GetCurrentUserId();

            try
            {
                var booking = await _bookingService.CreateBookingAsync(request, customerId, cancellationToken);
                return StatusCode(StatusCodes.Status201Created,
                    ApiResponse<BookingResponse>.SuccessResult(booking, "Booking request created successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Get bookings for the authenticated customer. CUSTOMER role only.</summary>
        [HttpGet("my")]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookingResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
        {
            var customerId = GetCurrentUserId();
            var bookings = await _bookingService.GetCustomerBookingsAsync(customerId, cancellationToken);
            return Ok(ApiResponse<IEnumerable<BookingResponse>>.SuccessResult(bookings));
        }

        /// <summary>Get assigned bookings for the authenticated provider. PROVIDER role only.</summary>
        [HttpGet("provider")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookingResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProviderBookings(CancellationToken cancellationToken)
        {
            var providerUserId = GetCurrentUserId();
            try
            {
                var bookings = await _bookingService.GetProviderBookingsAsync(providerUserId, cancellationToken);
                return Ok(ApiResponse<IEnumerable<BookingResponse>>.SuccessResult(bookings));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Get completed/finished booking history for the authenticated customer. CUSTOMER role only.</summary>
        [HttpGet("history")]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BookingResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomerHistory(CancellationToken cancellationToken)
        {
            var customerId = GetCurrentUserId();
            var history = await _bookingService.GetCustomerHistoryAsync(customerId, cancellationToken);
            return Ok(ApiResponse<IEnumerable<BookingResponse>>.SuccessResult(history));
        }

        /// <summary>Get a booking by ID. CUSTOMER, PROVIDER, and ADMIN roles.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "CUSTOMER,PROVIDER,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<BookingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingById(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin, cancellationToken);
                if (booking == null)
                    return NotFound(ApiResponse<object>.FailureResult($"Booking '{id}' not found."));

                return Ok(ApiResponse<BookingResponse>.SuccessResult(booking));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Accept a pending booking. PROVIDER role only.</summary>
        [HttpPatch("{id:guid}/accept")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<BookingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptBooking(Guid id, CancellationToken cancellationToken)
        {
            var providerUserId = GetCurrentUserId();
            try
            {
                var result = await _bookingService.AcceptBookingAsync(id, providerUserId, cancellationToken);
                return Ok(ApiResponse<BookingResponse>.SuccessResult(result, "Booking accepted successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Reject a pending booking. PROVIDER role only.</summary>
        [HttpPatch("{id:guid}/reject")]
        [Authorize(Roles = "PROVIDER")]
        [ProducesResponseType(typeof(ApiResponse<BookingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectBooking(Guid id, CancellationToken cancellationToken)
        {
            var providerUserId = GetCurrentUserId();
            try
            {
                var result = await _bookingService.RejectBookingAsync(id, providerUserId, cancellationToken);
                return Ok(ApiResponse<BookingResponse>.SuccessResult(result, "Booking rejected."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Cancel a booking before acceptance. CUSTOMER role only.</summary>
        [HttpPatch("{id:guid}/cancel")]
        [Authorize(Roles = "CUSTOMER")]
        [ProducesResponseType(typeof(ApiResponse<BookingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelBooking(Guid id, CancellationToken cancellationToken)
        {
            var customerId = GetCurrentUserId();
            try
            {
                var result = await _bookingService.CancelBookingAsync(id, customerId, cancellationToken);
                return Ok(ApiResponse<BookingResponse>.SuccessResult(result, "Booking cancelled."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        /// <summary>Complete an accepted booking. CUSTOMER, PROVIDER, and ADMIN roles.</summary>
        [HttpPatch("{id:guid}/complete")]
        [Authorize(Roles = "CUSTOMER,PROVIDER,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<BookingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteBooking(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            try
            {
                var result = await _bookingService.CompleteBookingAsync(id, userId, isAdmin, cancellationToken);
                return Ok(ApiResponse<BookingResponse>.SuccessResult(result, "Booking marked as completed."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.FailureResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.FailureResult(ex.Message));
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException("User ID claim missing.");
            return Guid.Parse(claim);
        }

        private bool IsAdmin()
        {
            return User.IsInRole("ADMIN");
        }
    }
}
