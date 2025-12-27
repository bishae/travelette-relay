using Travelette.Relay.Domain.Entities;

namespace Travelette.Relay.Domain.Repositories;

public interface ITripRepository
{
    Task<IEnumerable<Trip>> GetAllAsync();
    Task<Trip?> GetByIdAsync(Guid id);
    Task<Trip> AddAsync(Trip trip);
    Task UpdateAsync(Trip trip);
    Task DeleteAsync(Guid id);
}

