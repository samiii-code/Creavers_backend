using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Chat;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>In-app chat for booking participants.</summary>
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    [Produces("application/json")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IValidator<SendChatMessageRequest> _validator;

        public ChatController(IChatService chatService, IValidator<SendChatMessageRequest> validator)
        {
            _chatService = chatService;
            _validator   = validator;
        }

        // ─── POST /api/chat/{bookingId} ─────────────────────────────────────────
        /// <summary>Send a chat message to the other participant of a booking.</summary>
        /// <param name="bookingId">The booking the chat belongs to.</param>
        [HttpPost("{bookingId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ChatMessageResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendMessage(
            Guid bookingId, [FromBody] SendChatMessageRequest request, CancellationToken ct)
        {
            var validation = await _validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return BadRequest(ApiResponse<object>.FailureResult(
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
            try
            {
                var result = await _chatService.SendMessageAsync(bookingId, GetCurrentUserId(), request.Message, ct);
                return StatusCode(StatusCodes.Status201Created,
                    ApiResponse<ChatMessageResponse>.SuccessResult(result, "Message sent."));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── GET /api/chat/{bookingId} ──────────────────────────────────────────
        /// <summary>Get all messages for a booking (participants only).</summary>
        [HttpGet("{bookingId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ChatMessageResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMessages(Guid bookingId, CancellationToken ct)
        {
            try
            {
                var result = await _chatService.GetMessagesAsync(bookingId, GetCurrentUserId(), ct);
                return Ok(ApiResponse<IEnumerable<ChatMessageResponse>>.SuccessResult(result));
            }
            catch (KeyNotFoundException ex)     { return NotFound(ApiResponse<object>.FailureResult(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.FailureResult(ex.Message)); }
        }

        // ─── Helper ──────────────────────────────────────────────────────────────
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException("User ID claim missing.");
            return Guid.Parse(claim);
        }
    }
}
