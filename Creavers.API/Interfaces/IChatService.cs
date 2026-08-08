using Creavers.API.DTOs.Chat;

namespace Creavers.API.Interfaces
{
    public interface IChatService
    {
        Task<ChatMessageResponse> SendMessageAsync(Guid bookingId, Guid senderId, string message, CancellationToken ct = default);
        Task<IEnumerable<ChatMessageResponse>> GetMessagesAsync(Guid bookingId, Guid requestingUserId, CancellationToken ct = default);
    }
}
