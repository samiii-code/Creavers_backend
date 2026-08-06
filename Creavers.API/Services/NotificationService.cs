using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Data;
using Creavers.API.DTOs.Notifications;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<NotificationResponse> CreateNotificationAsync(
            Guid userId,
            string title,
            string message,
            CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
            return _mapper.Map<NotificationResponse>(notification);
        }

        public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
        }

        public async Task MarkAsReadAsync(
            Guid notificationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Notification '{notificationId}' not found.");

            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;

            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, cancellationToken);
        }
    }
}
