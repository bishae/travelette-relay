using Travelette.Relay.Application.DTOs;

namespace Travelette.Relay.Application.Services;

public interface IBookingService
{
    Task<IEnumerable<BookingDto>> GetBookingsAsync();
    Task<BookingDto?> GetBookingByIdAsync(Guid id);
}

