using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Travelette.Relay.Application.DTOs;
using Travelette.Relay.Application.Services;

namespace Travelette.Relay.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IPaymentApplicationService _paymentService;
    private readonly ILogger<TripsController> _logger;

    public TripsController(
        ITripService tripService,
        IPaymentApplicationService paymentService,
        ILogger<TripsController> logger)
    {
        _tripService = tripService;
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips([FromQuery] string? lang = "en")
    {
        var language = ValidateLanguage(lang);
        var trips = await _tripService.GetTripsAsync(language);
        return Ok(trips);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TripDto>> GetTrip(Guid id, [FromQuery] string? lang = "en")
    {
        var language = ValidateLanguage(lang);
        var trip = await _tripService.GetTripByIdAsync(id, language);
        
        if (trip == null)
        {
            return NotFound();
        }

        return Ok(trip);
    }

    [HttpPost]
    public async Task<ActionResult<TripDto>> CreateTrip(CreateTripDto dto, [FromQuery] string? lang = "en")
    {
        var language = ValidateLanguage(lang);
        var trip = await _tripService.CreateTripAsync(dto, language);
        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, trip);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrip(Guid id, CreateTripDto dto, [FromQuery] string? lang = "en")
    {
        var language = ValidateLanguage(lang);
        // Convert CreateTripDto to UpdateTripDto (they're identical in structure)
        var updateDto = new UpdateTripDto
        {
            Title = dto.Title ?? new Dictionary<string, string>(),
            Location = dto.Location ?? new Dictionary<string, string>(),
            StartDate = dto.StartDate,
            DurationDays = dto.DurationDays,
            Price = dto.Price,
            Description = dto.Description ?? new Dictionary<string, string>(),
            HeroImage = dto.HeroImage,
            Gallery = dto.Gallery ?? new List<string>(),
            Itinerary = dto.Itinerary ?? new List<CreateItineraryDayDto>(),
            Inclusions = dto.Inclusions ?? new List<Dictionary<string, string>>(),
            Exclusions = dto.Exclusions ?? new List<Dictionary<string, string>>(),
            SpotsTotal = dto.SpotsTotal,
            SpotsLeft = dto.SpotsLeft
        };
        var trip = await _tripService.UpdateTripAsync(id, updateDto, language);
        
        if (trip == null)
        {
            return NotFound();
        }

        return Ok(trip);
    }

    [HttpPost("{id}/refund-all")]
    public async Task<IActionResult> RefundAllBookings(Guid id, [FromBody] RefundAllBookingsDto? dto = null)
    {
        try
        {
            var result = await _paymentService.RefundAllBookingsForTripAsync(id, dto?.Reason);
            return Ok(new
            {
                success = true,
                totalBookingsRefunded = result.TotalBookingsRefunded,
                totalSpotsRefunded = result.TotalSpotsRefunded,
                totalAmountRefunded = result.TotalAmountRefunded,
                message = $"Successfully refunded {result.TotalBookingsRefunded} booking(s). {result.TotalSpotsRefunded} spot(s) refunded, ${result.TotalAmountRefunded:N2} total."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation refunding all bookings for trip {TripId}", id);
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ex.Message);
            }
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding all bookings for trip {TripId}", id);
            return StatusCode(500, new { error = "Error processing refunds", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        try
        {
            var deleted = await _tripService.DeleteTripAsync(id);
            
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete trip {TripId}: {Message}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting trip {TripId}", id);
            return StatusCode(500, new { error = "Error deleting trip", details = ex.Message });
        }
    }

    private static string ValidateLanguage(string? lang)
    {
        if (string.IsNullOrEmpty(lang))
            return "en";
        
        lang = lang.ToLower().Trim();
        
        return lang switch
        {
            "en" => "en",
            "ar" => "ar",
            "fr" => "fr",
            _ => "en"
        };
    }
}

