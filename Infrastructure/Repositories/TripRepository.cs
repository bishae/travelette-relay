using Microsoft.EntityFrameworkCore;
using Travelette.Relay.Data;
using Travelette.Relay.Domain.Entities;
using Travelette.Relay.Domain.Repositories;

namespace Travelette.Relay.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly AppDbContext _context;

    public TripRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Trip>> GetAllAsync()
    {
        return await _context.Trips
            .Include(t => t.Itinerary.OrderBy(i => i.Day))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Trip?> GetByIdAsync(Guid id)
    {
        return await _context.Trips
            .Include(t => t.Itinerary.OrderBy(i => i.Day))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Trip> AddAsync(Trip trip)
    {
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();
        return trip;
    }

    public async Task UpdateAsync(Trip trip)
    {
        // Remove existing itinerary that's not in the new list
        var existingItineraryIds = trip.Itinerary.Select(i => i.Id).ToList();
        var itineraryToRemove = await _context.ItineraryDays
            .Where(i => i.TripId == trip.Id && !existingItineraryIds.Contains(i.Id))
            .ToListAsync();
        
        if (itineraryToRemove.Any())
        {
            _context.ItineraryDays.RemoveRange(itineraryToRemove);
        }

        // Add or update itinerary items
        foreach (var itineraryDay in trip.Itinerary)
        {
            if (itineraryDay.Id == default(Guid))
            {
                // New item
                itineraryDay.TripId = trip.Id;
                _context.ItineraryDays.Add(itineraryDay);
            }
            else
            {
                // Existing item - update it
                _context.ItineraryDays.Update(itineraryDay);
            }
        }

        _context.Trips.Update(trip);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip != null)
        {
            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
        }
    }
}

