namespace Travelette.Relay.DTOs;

public class CreatePaymentIntentDto
{
    public Guid TripId { get; set; }
    public int Spots { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
}

public class PaymentIntentResponseDto
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

