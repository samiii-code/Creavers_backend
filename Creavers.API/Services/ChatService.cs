using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Data;
using Creavers.API.DTOs.Chat;
using Creavers.API.Interfaces;
using Creavers.API.Models;

namespace Creavers.API.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ApplicationDbContext context, IMapper mapper, ILogger<ChatService> logger)
        {
            _context = context;
            _mapper  = mapper;
            _logger  = logger;
        }

        // ── SEND MESSAGE ─────────────────────────────────────────────────────────
        public async Task<ChatMessageResponse> SendMessageAsync(
            Guid bookingId, Guid senderId, string message, CancellationToken ct = default)
        {
            // Verify sender is a participant of the booking
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            bool isParticipant = booking.CustomerId == senderId
                              || booking.Provider.ApplicationUserId == senderId;
            if (!isParticipant)
                throw new UnauthorizedAccessException("You are not a participant of this booking.");

            var chatMessage = new ChatMessage
            {
                BookingId = bookingId,
                SenderId  = senderId,
                Message   = message,
                SentAt    = DateTime.UtcNow,
                IsRead    = false
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Chat message sent in Booking {BookingId} by {SenderId}", bookingId, senderId);

            // Reload with sender navigation for mapping
            var loaded = await _context.ChatMessages
                .Include(m => m.Sender)
                .FirstAsync(m => m.Id == chatMessage.Id, ct);

            return _mapper.Map<ChatMessageResponse>(loaded);
        }

        // ── GET MESSAGES ──────────────────────────────────────────────────────────
        public async Task<IEnumerable<ChatMessageResponse>> GetMessagesAsync(
            Guid bookingId, Guid requestingUserId, CancellationToken ct = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.Provider)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");

            bool isParticipant = booking.CustomerId == requestingUserId
                              || booking.Provider.ApplicationUserId == requestingUserId;
            if (!isParticipant)
                throw new UnauthorizedAccessException("You are not a participant of this booking.");

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.BookingId == bookingId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<ChatMessageResponse>>(messages);
        }
    }
}
