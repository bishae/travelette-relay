using Microsoft.EntityFrameworkCore;
using Travelette.Relay.Data;
using Travelette.Relay.Domain.Entities;
using Travelette.Relay.Domain.Repositories;

namespace Travelette.Relay.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await _context.Bookings
            .Include(b => b.Trip)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _context.Bookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking?> GetByStripePaymentIntentIdAsync(string paymentIntentId)
    {
        return await _context.Bookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.StripePaymentIntentId == paymentIntentId);
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
    }
}

