using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Creavers.API.Data;
using Creavers.API.DTOs.Bookings;
using Creavers.API.Interfaces;
using Creavers.API.Models;
using Creavers.API.Models.Enums;

namespace Creavers.API.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<BookingService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        // ── 1. CREATE BOOKING ──────────────────────────────────────────────────
        public async Task<BookingResponse> CreateBookingAsync(
            CreateBookingRequest request,
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            // Validate Task
            var task = await _context.CustomerTasks
                .FirstOrDefaultAsync(t => t.Id == request.TaskId && !t.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Task '{request.TaskId}' not found.");

            if (task.CustomerId != customerId)
                throw new UnauthorizedAccessException("You can only create bookings for your own tasks.");

            // Validate Provider
            var provider = await _context.ProviderProfiles
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId && !p.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Provider profile '{request.ProviderId}' not found.");

            if (provider.Status != ProviderStatus.Approved)
                throw new InvalidOperationException("Cannot book a provider who is not approved.");

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                ProviderId = request.ProviderId,
                CustomerId = customerId,
                BookingStatus = BookingStatus.Pending,
                Notes = request.Notes,
                ScheduledDate = request.ScheduledDate,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Bookings.AddAsync(booking, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking created: {BookingId} for Task {TaskId} and Provider {ProviderId}",
                booking.Id, request.TaskId, request.ProviderId);

            // Send notification to Provider
            await _notificationService.CreateNotificationAsync(
                provider.ApplicationUserId,
                "New Booking Request",
                $"You have received a new booking request for task '{task.Title}'.",
                cancellationToken);

            return await GetBookingResponseByIdAsync(booking.Id, cancellationToken)
                ?? throw new InvalidOperationException("Failed to retrieve booking after creation.");
        }

        // ── 2. GET CUSTOMER BOOKINGS ──────────────────────────────────────────
        public async Task<IEnumerable<BookingResponse>> GetCustomerBookingsAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Task)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Customer)
                .Where(b => b.CustomerId == customerId && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<BookingResponse>>(bookings);
        }

        // ── 3. GET PROVIDER BOOKINGS ──────────────────────────────────────────
        public async Task<IEnumerable<BookingResponse>> GetProviderBookingsAsync(
            Guid providerUserId,
            CancellationToken cancellationToken = default)
        {
            var providerProfile = await _context.ProviderProfiles
                .FirstOrDefaultAsync(p => p.ApplicationUserId == providerUserId && !p.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Provider profile not found for user.");

            var bookings = await _context.Bookings
                .Include(b => b.Task)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Customer)
                .Where(b => b.ProviderId == providerProfile.Id && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<BookingResponse>>(bookings);
        }

        // ── 4. GET BOOKING BY ID ──────────────────────────────────────────────
        public async Task<BookingResponse?> GetBookingByIdAsync(
            Guid bookingId,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var booking = await _context.Bookings
                .Include(b => b.Task)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, cancellationToken);

            if (booking == null)
                return null;

            if (!isAdmin && booking.CustomerId != userId && booking.Provider.ApplicationUserId != userId)
                throw new UnauthorizedAccessException("You are not authorized to view this booking.");

            return _mapper.Map<BookingResponse>(booking);
        }

        // ── 5. ACCEPT BOOKING (Provider) ──────────────────────────────────────
        public async Task<BookingResponse> AcceptBookingAsync(
            Guid bookingId,
            Guid providerUserId,
            CancellationToken cancellationToken = default)
        {
            var booking = await GetBookingEntityAsync(bookingId, cancellationToken);

            if (booking.Provider.ApplicationUserId != providerUserId)
                throw new UnauthorizedAccessException("Only the assigned provider can accept this booking.");

            if (booking.BookingStatus != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot accept booking with status '{booking.BookingStatus}'. Only Pending bookings can be accepted.");

            booking.BookingStatus = BookingStatus.Accepted;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} accepted by provider {UserId}", bookingId, providerUserId);

            // Notify Customer
            await _notificationService.CreateNotificationAsync(
                booking.CustomerId,
                "Booking Accepted",
                $"Your booking for task '{booking.Task.Title}' has been accepted by the service provider.",
                cancellationToken);

            return _mapper.Map<BookingResponse>(booking);
        }

        // ── 6. REJECT BOOKING (Provider) ──────────────────────────────────────
        public async Task<BookingResponse> RejectBookingAsync(
            Guid bookingId,
            Guid providerUserId,
            CancellationToken cancellationToken = default)
        {
            var booking = await GetBookingEntityAsync(bookingId, cancellationToken);

            if (booking.Provider.ApplicationUserId != providerUserId)
                throw new UnauthorizedAccessException("Only the assigned provider can reject this booking.");

            if (booking.BookingStatus != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot reject booking with status '{booking.BookingStatus}'. Only Pending bookings can be rejected.");

            booking.BookingStatus = BookingStatus.Rejected;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} rejected by provider {UserId}", bookingId, providerUserId);

            // Notify Customer
            await _notificationService.CreateNotificationAsync(
                booking.CustomerId,
                "Booking Rejected",
                $"Your booking request for task '{booking.Task.Title}' was declined by the service provider.",
                cancellationToken);

            return _mapper.Map<BookingResponse>(booking);
        }

        // ── 7. CANCEL BOOKING (Customer) ──────────────────────────────────────
        public async Task<BookingResponse> CancelBookingAsync(
            Guid bookingId,
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var booking = await GetBookingEntityAsync(bookingId, cancellationToken);

            if (booking.CustomerId != customerId)
                throw new UnauthorizedAccessException("Only the customer who created the booking can cancel it.");

            // Customer can cancel BEFORE acceptance
            if (booking.BookingStatus != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot cancel booking with status '{booking.BookingStatus}'. Bookings can only be cancelled before acceptance (when Pending).");

            booking.BookingStatus = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} cancelled by customer {CustomerId}", bookingId, customerId);

            // Notify Provider
            await _notificationService.CreateNotificationAsync(
                booking.Provider.ApplicationUserId,
                "Booking Cancelled",
                $"The booking for task '{booking.Task.Title}' has been cancelled by the customer.",
                cancellationToken);

            return _mapper.Map<BookingResponse>(booking);
        }

        // ── 8. COMPLETE BOOKING (Customer or Provider) ────────────────────────
        public async Task<BookingResponse> CompleteBookingAsync(
            Guid bookingId,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var booking = await GetBookingEntityAsync(bookingId, cancellationToken);

            if (!isAdmin && booking.CustomerId != userId && booking.Provider.ApplicationUserId != userId)
                throw new UnauthorizedAccessException("You are not authorized to mark this booking as completed.");

            // Only accepted bookings can become completed
            if (booking.BookingStatus != BookingStatus.Accepted)
                throw new InvalidOperationException($"Cannot complete booking with status '{booking.BookingStatus}'. Only Accepted bookings can be completed.");

            booking.BookingStatus = BookingStatus.Completed;
            booking.UpdatedAt = DateTime.UtcNow;

            // Also update underlying customer task status to Completed
            if (booking.Task != null)
            {
                booking.Task.Status = CustomerTaskStatus.Completed;
                booking.Task.UpdatedAt = DateTime.UtcNow;
                _context.CustomerTasks.Update(booking.Task);
            }

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} marked as completed", bookingId);

            // Notify counterparty
            var recipientId = userId == booking.CustomerId
                ? booking.Provider.ApplicationUserId
                : booking.CustomerId;

            await _notificationService.CreateNotificationAsync(
                recipientId,
                "Booking Completed",
                $"The booking for task '{booking.Task?.Title}' has been marked as completed.",
                cancellationToken);

            return _mapper.Map<BookingResponse>(booking);
        }

        // ── 9. CUSTOMER HISTORY ──────────────────────────────────────────────
        public async Task<IEnumerable<BookingResponse>> GetCustomerHistoryAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var historyStatuses = new[] { BookingStatus.Completed, BookingStatus.Cancelled, BookingStatus.Rejected };

            var bookings = await _context.Bookings
                .Include(b => b.Task)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Customer)
                .Where(b => b.CustomerId == customerId && historyStatuses.Contains(b.BookingStatus) && !b.IsDeleted)
                .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<BookingResponse>>(bookings);
        }

        // ── 10. PROVIDER HISTORY ─────────────────────────────────────────────
        public async Task<IEnumerable<BookingResponse>> GetProviderHistoryAsync(
            Guid providerUserId,
            CancellationToken cancellationToken = default)
        {
            var providerProfile = await _context.ProviderProfiles
                .FirstOrDefaultAsync(p => p.ApplicationUserId == providerUserId && !p.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Provider profile not found for user.");

            var historyStatuses = new[] { BookingStatus.Completed, BookingStatus.Cancelled, BookingStatus.Rejected };

            var bookings = await _context.Bookings
                .Include(b => b.Task)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Customer)
                .Where(b => b.ProviderId == providerProfile.Id && historyStatuses.Contains(b.BookingStatus) && !b.IsDeleted)
                .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<BookingResponse>>(bookings);
        }

        // ── PRIVATE HELPERS ──────────────────────────────────────────────────
        private async Task<Booking> GetBookingEntityAsync(Guid bookingId, CancellationToken cancellationToken)
        {
            return await _context.Bookings
                .Include(b => b.Task)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");
        }

        private async Task<BookingResponse?> GetBookingResponseByIdAsync(Guid bookingId, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Task)
                .Include(b => b.Provider).ThenInclude(p => p.ApplicationUser)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted, cancellationToken);

            return booking == null ? null : _mapper.Map<BookingResponse>(booking);
        }
    }
}
