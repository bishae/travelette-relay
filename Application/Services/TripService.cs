using Microsoft.Extensions.Logging;
using Travelette.Relay.Application.DTOs;
using Travelette.Relay.Domain.Entities;
using Travelette.Relay.Domain.Repositories;

namespace Travelette.Relay.Application.Services;

public class TripService : ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<TripService> _logger;
    private readonly IConfigurationService _configurationService;

    public TripService(
        ITripRepository tripRepository,
        IBookingRepository bookingRepository,
        ILogger<TripService> logger,
        IConfigurationService configurationService)
    {
        _tripRepository = tripRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
        _configurationService = configurationService;
    }

    public async Task<IEnumerable<TripDto>> GetTripsAsync(string language = "en")
    {
        var trips = await _tripRepository.GetAllAsync();
        return trips.Select(t => MapToDto(t, language));
    }

    public async Task<TripDto?> GetTripByIdAsync(Guid id, string language = "en")
    {
        var trip = await _tripRepository.GetByIdAsync(id);
        return trip != null ? MapToDto(trip, language) : null;
    }

    public async Task<TripDto> CreateTripAsync(CreateTripDto dto, string language = "en")
    {
        var trip = new Trip
        {
            Title = System.Text.Json.JsonSerializer.Serialize(dto.Title ?? new Dictionary<string, string>()),
            Location = System.Text.Json.JsonSerializer.Serialize(dto.Location ?? new Dictionary<string, string>()),
            StartDate = dto.StartDate,
            DurationDays = dto.DurationDays,
            Price = dto.Price,
            Description = System.Text.Json.JsonSerializer.Serialize(dto.Description ?? new Dictionary<string, string>()),
            HeroImage = dto.HeroImage,
            Gallery = dto.Gallery ?? new List<string>(),
            Inclusions = System.Text.Json.JsonSerializer.Serialize(dto.Inclusions ?? new List<Dictionary<string, string>>()),
            Exclusions = System.Text.Json.JsonSerializer.Serialize(dto.Exclusions ?? new List<Dictionary<string, string>>()),
            SpotsTotal = dto.SpotsTotal,
            SpotsLeft = dto.SpotsTotal,
            Itinerary = (dto.Itinerary ?? new List<CreateItineraryDayDto>()).Select(i => new ItineraryDay
            {
                Day = i.Day,
                Title = System.Text.Json.JsonSerializer.Serialize(i.Title ?? new Dictionary<string, string>()),
                Activity = System.Text.Json.JsonSerializer.Serialize(i.Activity ?? new Dictionary<string, string>())
            }).ToList()
        };

        var createdTrip = await _tripRepository.AddAsync(trip);
        return MapToDto(createdTrip, language);
    }

    public async Task<TripDto?> UpdateTripAsync(Guid id, UpdateTripDto dto, string language = "en")
    {
        var trip = await _tripRepository.GetByIdAsync(id);
        if (trip == null)
        {
            return null;
        }

        trip.Title = System.Text.Json.JsonSerializer.Serialize(dto.Title ?? new Dictionary<string, string>());
        trip.Location = System.Text.Json.JsonSerializer.Serialize(dto.Location ?? new Dictionary<string, string>());
        trip.StartDate = dto.StartDate;
        trip.DurationDays = dto.DurationDays;
        trip.Price = dto.Price;
        trip.Description = System.Text.Json.JsonSerializer.Serialize(dto.Description ?? new Dictionary<string, string>());
        trip.HeroImage = dto.HeroImage;
        trip.Gallery = dto.Gallery ?? new List<string>();
        trip.Inclusions = System.Text.Json.JsonSerializer.Serialize(dto.Inclusions ?? new List<Dictionary<string, string>>());
        trip.Exclusions = System.Text.Json.JsonSerializer.Serialize(dto.Exclusions ?? new List<Dictionary<string, string>>());
        trip.SpotsTotal = dto.SpotsTotal;
        trip.SpotsLeft = dto.SpotsLeft;
        trip.UpdatedAt = DateTime.UtcNow;

        // Remove existing itinerary
        if (trip.Itinerary != null && trip.Itinerary.Any())
        {
            trip.Itinerary.Clear();
        }

        // Add new itinerary items
        if (dto.Itinerary != null && dto.Itinerary.Any())
        {
            trip.Itinerary = dto.Itinerary.Select(i => new ItineraryDay
            {
                Id = default(Guid), // Set to default so repository can detect as new item
                TripId = trip.Id,
                Day = i.Day,
                Title = System.Text.Json.JsonSerializer.Serialize(i.Title ?? new Dictionary<string, string>()),
                Activity = System.Text.Json.JsonSerializer.Serialize(i.Activity ?? new Dictionary<string, string>())
            }).ToList();
        }
        else
        {
            trip.Itinerary = new List<ItineraryDay>();
        }

        await _tripRepository.UpdateAsync(trip);
        return MapToDto(trip, language);
    }

    public async Task<bool> DeleteTripAsync(Guid id)
    {
        var trip = await _tripRepository.GetByIdAsync(id);
        if (trip == null)
        {
            return false;
        }

        // Check if there are any paid bookings (reserved spots) for this trip
        var bookings = await _bookingRepository.GetByTripIdAsync(id);
        var paidBookings = bookings.Where(b => b.Status == BookingStatus.Paid).ToList();
        
        if (paidBookings.Any())
        {
            var totalReservedSpots = paidBookings.Sum(b => b.SpotsReserved);
            throw new InvalidOperationException(
                $"Cannot delete trip. There are {totalReservedSpots} reserved spot(s) ({paidBookings.Count} paid booking(s)). " +
                "Please refund all customers before deleting the trip.");
        }

        // Delete all bookings (including refunded ones) before deleting the trip
        // This is necessary because of the foreign key constraint (DeleteBehavior.Restrict)
        await _bookingRepository.DeleteByTripIdAsync(id);

        await _tripRepository.DeleteAsync(id);
        return true;
    }

    private TripDto MapToDto(Trip trip, string language = "en")
    {
        var endDate = trip.StartDate.AddDays(trip.DurationDays - 1);
        var dateString = FormatDateRange(trip.StartDate, endDate);
        var durationString = FormatDuration(trip.DurationDays);
        var currencySymbol = _configurationService.GetCurrencySymbol();
        var priceString = FormatPrice(trip.Price, currencySymbol);

        return new TripDto
        {
            Id = trip.Id,
            Title = trip.GetTitle(language),
            Location = trip.GetLocation(language),
            Date = dateString,
            Duration = durationString,
            StartDate = trip.StartDate,
            DurationDays = trip.DurationDays,
            Price = priceString,
            PriceAmount = trip.Price,
            Description = trip.GetDescription(language),
            HeroImage = trip.HeroImage,
            Gallery = trip.Gallery,
            Itinerary = trip.Itinerary.OrderBy(i => i.Day).Select(i => new ItineraryDayDto
            {
                Id = i.Id,
                Day = i.Day,
                Title = i.GetTitle(language),
                Activity = i.GetActivity(language)
            }).ToList(),
            Inclusions = trip.GetInclusions(language),
            Exclusions = trip.GetExclusions(language),
            SpotsTotal = trip.SpotsTotal,
            SpotsLeft = trip.SpotsLeft,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt
        };
    }

    private static string FormatDateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate.Year == endDate.Year && startDate.Month == endDate.Month)
        {
            return $"{startDate:MMMM d} - {endDate:d}, {startDate:yyyy}";
        }
        else if (startDate.Year == endDate.Year)
        {
            return $"{startDate:MMMM d} - {endDate:MMMM d}, {startDate:yyyy}";
        }
        else
        {
            return $"{startDate:MMMM d, yyyy} - {endDate:MMMM d, yyyy}";
        }
    }

    private static string FormatDuration(int days)
    {
        var nights = days - 1;
        if (days == 1)
        {
            return "1 Day";
        }
        else if (nights == 1)
        {
            return $"{days} Days / {nights} Night";
        }
        else
        {
            return $"{days} Days / {nights} Nights";
        }
    }

    private static string FormatPrice(decimal price, string currencySymbol)
    {
        return $"{currencySymbol}{price:N2}";
    }
}

public interface IConfigurationService
{
    string GetCurrencySymbol();
    string GetCurrency();
}

