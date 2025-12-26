using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travelette.Relay.Data;
using Travelette.Relay.DTOs;
using Travelette.Relay.Models;

namespace Travelette.Relay.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(AppDbContext context, ILogger<BookingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetBookings()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Trip)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetBooking(Guid id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(booking));
    }

    private static BookingDto MapToDto(Booking booking)
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

