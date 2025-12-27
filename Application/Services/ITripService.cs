using Travelette.Relay.Application.DTOs;

namespace Travelette.Relay.Application.Services;

public interface ITripService
{
    Task<IEnumerable<TripDto>> GetTripsAsync(string language = "en");
    Task<TripDto?> GetTripByIdAsync(Guid id, string language = "en");
    Task<TripDto> CreateTripAsync(CreateTripDto dto, string language = "en");
    Task<TripDto?> UpdateTripAsync(Guid id, UpdateTripDto dto, string language = "en");
    Task<bool> DeleteTripAsync(Guid id);
}

