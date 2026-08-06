using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Creavers.API.DTOs;
using Creavers.API.DTOs.Notifications;
using Creavers.API.Interfaces;

namespace Creavers.API.Controllers
{
    /// <summary>Notification management endpoints.</summary>
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>Get all notifications for the authenticated user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyNotifications(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, cancellationToken);
            return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResult(notifications));
        }

        /// <summary>Get the count of unread notifications for the authenticated user.</summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
            return Ok(ApiResponse<int>.SuccessResult(count));
        }

        /// <summary>Mark a specific notification as read.</summary>
        [HttpPatch("{id:guid}/read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            try
            {
                await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
                return Ok(ApiResponse<object?>.SuccessResult(null, "Notification marked as read."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.FailureResult(ex.Message));
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
    }
}
