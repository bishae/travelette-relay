using Travelette.Relay.Domain.Entities;

namespace Travelette.Relay.Domain.Repositories;

public interface IBookingRepository
{
    Task<IEnumerable<Booking>> GetAllAsync();
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking?> GetByStripePaymentIntentIdAsync(string paymentIntentId);
    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
}

