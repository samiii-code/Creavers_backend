using Creavers.API.DTOs.Bookings;

namespace Creavers.API.Interfaces
{
    public interface IBookingService
    {
        Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, Guid customerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<BookingResponse>> GetCustomerBookingsAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<BookingResponse>> GetProviderBookingsAsync(Guid providerUserId, CancellationToken cancellationToken = default);
        Task<BookingResponse?> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<BookingResponse> AcceptBookingAsync(Guid bookingId, Guid providerUserId, CancellationToken cancellationToken = default);
        Task<BookingResponse> RejectBookingAsync(Guid bookingId, Guid providerUserId, CancellationToken cancellationToken = default);
        Task<BookingResponse> CancelBookingAsync(Guid bookingId, Guid customerId, CancellationToken cancellationToken = default);
        Task<BookingResponse> CompleteBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<IEnumerable<BookingResponse>> GetCustomerHistoryAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<BookingResponse>> GetProviderHistoryAsync(Guid providerUserId, CancellationToken cancellationToken = default);
    }
}
