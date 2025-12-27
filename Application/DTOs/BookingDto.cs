namespace Travelette.Relay.Application.DTOs;

public class BookingDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string TripTitle { get; set; } = string.Empty;
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int SpotsReserved { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountRefunded { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RefundBookingDto
{
    public Guid BookingId { get; set; }
    public string? Reason { get; set; }
    public decimal? Amount { get; set; }
    public int? SpotsToRefund { get; set; }
}

