using Travelette.Relay.Application.DTOs;
using Travelette.Relay.Domain.Repositories;

namespace Travelette.Relay.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<IEnumerable<BookingDto>> GetBookingsAsync()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        return bookings.Select(MapToDto);
    }

    public async Task<BookingDto?> GetBookingByIdAsync(Guid id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        return booking != null ? MapToDto(booking) : null;
    }

    private static BookingDto MapToDto(Domain.Entities.Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            TripId = booking.TripId,
            TripTitle = booking.Trip?.GetTitle("en") ?? "Unknown Trip",
            StripePaymentIntentId = booking.StripePaymentIntentId,
            CustomerEmail = booking.CustomerEmail,
            CustomerName = booking.CustomerName,
            SpotsReserved = booking.SpotsReserved,
            AmountPaid = booking.AmountPaid,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };
    }
}

