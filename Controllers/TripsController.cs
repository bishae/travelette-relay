using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travelette.Relay.Data;
using Travelette.Relay.DTOs;
using Travelette.Relay.Models;

namespace Travelette.Relay.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TripsController> _logger;
    private readonly IConfiguration _configuration;

    public TripsController(AppDbContext context, ILogger<TripsController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips()
    {
        var trips = await _context.Trips
            .Include(t => t.Itinerary.OrderBy(i => i.Day))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(trips.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TripDto>> GetTrip(Guid id)
    {
        var trip = await _context.Trips
            .Include(t => t.Itinerary.OrderBy(i => i.Day))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(trip));
    }

    [HttpPost]
    public async Task<ActionResult<TripDto>> CreateTrip(CreateTripDto dto)
    {
        var trip = new Trip
        {
            Title = dto.Title,
            Location = dto.Location,
            StartDate = dto.StartDate,
            DurationDays = dto.DurationDays,
            Price = dto.Price,
            Description = dto.Description,
            HeroImage = dto.HeroImage,
            Gallery = dto.Gallery,
            Inclusions = dto.Inclusions,
            Exclusions = dto.Exclusions,
            SpotsTotal = dto.SpotsTotal,
            SpotsLeft = dto.SpotsTotal, // Set spots left to same as total on creation
            Itinerary = dto.Itinerary.Select(i => new ItineraryDay
            {
                Day = i.Day,
                Title = i.Title,
                Activity = i.Activity
            }).ToList()
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, MapToDto(trip));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrip(Guid id, UpdateTripDto dto)
    {
        var trip = await _context.Trips
            .Include(t => t.Itinerary)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null)
        {
            return NotFound();
        }

        trip.Title = dto.Title;
        trip.Location = dto.Location;
        trip.StartDate = dto.StartDate;
        trip.DurationDays = dto.DurationDays;
        trip.Price = dto.Price;
        trip.Description = dto.Description;
        trip.HeroImage = dto.HeroImage;
        trip.Gallery = dto.Gallery;
        trip.Inclusions = dto.Inclusions;
        trip.Exclusions = dto.Exclusions;
        trip.SpotsTotal = dto.SpotsTotal;
        trip.SpotsLeft = dto.SpotsLeft;
        trip.UpdatedAt = DateTime.UtcNow;

        // Remove existing itinerary
        _context.ItineraryDays.RemoveRange(trip.Itinerary);

        // Add new itinerary
        trip.Itinerary = dto.Itinerary.Select(i => new ItineraryDay
        {
            TripId = trip.Id,
            Day = i.Day,
            Title = i.Title,
            Activity = i.Activity
        }).ToList();

        await _context.SaveChangesAsync();

        return Ok(MapToDto(trip));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private TripDto MapToDto(Trip trip)
    {
        var endDate = trip.StartDate.AddDays(trip.DurationDays - 1);
        var dateString = FormatDateRange(trip.StartDate, endDate);
        var durationString = FormatDuration(trip.DurationDays);
        var currencySymbol = _configuration["AppSettings:CurrencySymbol"] ?? "$";
        var priceString = FormatPrice(trip.Price, currencySymbol);

        return new TripDto
        {
            Id = trip.Id,
            Title = trip.Title,
            Location = trip.Location,
            Date = dateString,
            Duration = durationString,
            StartDate = trip.StartDate,
            DurationDays = trip.DurationDays,
            Price = priceString,
            PriceAmount = trip.Price,
            Description = trip.Description,
            HeroImage = trip.HeroImage,
            Gallery = trip.Gallery,
            Itinerary = trip.Itinerary.Select(i => new ItineraryDayDto
            {
                Id = i.Id,
                Day = i.Day,
                Title = i.Title,
                Activity = i.Activity
            }).ToList(),
            Inclusions = trip.Inclusions,
            Exclusions = trip.Exclusions,
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

