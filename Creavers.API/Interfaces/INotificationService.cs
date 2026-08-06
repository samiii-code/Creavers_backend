using Creavers.API.DTOs.Notifications;

namespace Creavers.API.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
        Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
