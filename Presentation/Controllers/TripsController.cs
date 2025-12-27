using Microsoft.AspNetCore.Mvc;
using Travelette.Relay.Application.DTOs;
using Travelette.Relay.Application.Services;

namespace Travelette.Relay.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
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
    public async Task<IActionResult> UpdateTrip(Guid id, UpdateTripDto dto, [FromQuery] string? lang = "en")
    {
        var language = ValidateLanguage(lang);
        var trip = await _tripService.UpdateTripAsync(id, dto, language);
        
        if (trip == null)
        {
            return NotFound();
        }

        return Ok(trip);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var deleted = await _tripService.DeleteTripAsync(id);
        
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
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

