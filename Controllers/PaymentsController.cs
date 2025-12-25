using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Travelette.Relay.Data;
using Travelette.Relay.DTOs;
using Travelette.Relay.Models;

namespace Travelette.Relay.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentsController(AppDbContext context, ILogger<PaymentsController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        
        // Initialize Stripe
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    [HttpPost("create-intent")]
    public async Task<ActionResult<PaymentIntentResponseDto>> CreatePaymentIntent(CreatePaymentIntentDto dto)
    {
        try
        {
            // Validate Stripe API key
            var stripeKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(stripeKey))
            {
                _logger.LogError("Stripe SecretKey is not configured");
                return StatusCode(500, "Payment service is not configured");
            }

            var trip = await _context.Trips.FindAsync(dto.TripId);
            if (trip == null)
            {
                _logger.LogWarning($"Trip not found: {dto.TripId}");
                return NotFound("Trip not found");
            }

            if (string.IsNullOrWhiteSpace(dto.CustomerEmail))
            {
                return BadRequest("Customer email is required");
            }

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                return BadRequest("Customer name is required");
            }

            if (dto.Spots <= 0)
            {
                return BadRequest("Number of spots must be greater than 0");
            }

            if (dto.Spots > trip.SpotsLeft)
            {
                return BadRequest($"Only {trip.SpotsLeft} spots available");
            }

            // Calculate total amount
            var totalAmount = trip.Price * dto.Spots;
            if (totalAmount <= 0)
            {
                _logger.LogError($"Invalid price for trip {trip.Id}: Price={trip.Price}, Spots={dto.Spots}, Total={totalAmount}");
                return BadRequest("Trip price must be greater than 0");
            }

            var amountInCents = (long)(totalAmount * 100); // Stripe uses cents
            if (amountInCents < 50) // Stripe minimum is $0.50
            {
                return BadRequest("Total amount must be at least $0.50");
            }

            _logger.LogInformation($"Creating payment intent: TripId={trip.Id}, Spots={dto.Spots}, Amount={totalAmount}, AmountInCents={amountInCents}");

            // Create payment intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = _configuration["AppSettings:Currency"]?.ToLower() ?? "usd",
                Metadata = new Dictionary<string, string>
                {
                    { "tripId", trip.Id.ToString() },
                    { "spots", dto.Spots.ToString() },
                    { "customerEmail", dto.CustomerEmail },
                    { "customerName", dto.CustomerName }
                },
                ReceiptEmail = dto.CustomerEmail,
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            // Create booking record
            var booking = new Booking
            {
                TripId = trip.Id,
                StripePaymentIntentId = paymentIntent.Id,
                CustomerEmail = dto.CustomerEmail,
                CustomerName = dto.CustomerName,
                SpotsReserved = dto.Spots,
                AmountPaid = totalAmount,
                Status = BookingStatus.Pending
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new PaymentIntentResponseDto
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Amount = totalAmount
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent: {Message}", ex.Message);
            return StatusCode(500, new { error = $"Stripe error: {ex.Message}", details = ex.StripeError?.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error creating payment intent", details = ex.Message });
        }
    }

    [HttpPost("webhook")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> HandleWebhook()
    {
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogError("Webhook secret is not configured");
            return StatusCode(500, "Webhook secret not configured");
        }

        if (string.IsNullOrWhiteSpace(stripeSignature))
        {
            _logger.LogWarning("Missing Stripe-Signature header");
            return BadRequest("Missing Stripe-Signature header");
        }

        string json;
        try
        {
            // Ensure body position is at start
            Request.Body.Position = 0;
            
            // Read the raw request body for signature verification
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            json = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading webhook request body: {Message}", ex.Message);
            return StatusCode(500, "Error reading request body");
        }

        try
        {
            _logger.LogInformation("Processing webhook event. Signature: {Signature}", stripeSignature.Substring(0, Math.Min(20, stripeSignature.Length)));
            
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                webhookSecret
                // API version mismatch handling: The library will now match the latest API version
            );

            _logger.LogInformation("Webhook event type: {Type}, ID: {Id}", stripeEvent.Type, stripeEvent.Id);

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await HandlePaymentSuccess(paymentIntent);
                }
            }
            else if (stripeEvent.Type == "payment_intent.payment_failed")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await HandlePaymentFailed(paymentIntent);
                }
            }
            else if (stripeEvent.Type == "payment_intent.created")
            {
                // Payment intent created - just acknowledge, don't process yet
                _logger.LogInformation("Payment intent created: {Id}", (stripeEvent.Data.Object as PaymentIntent)?.Id);
            }
            else
            {
                _logger.LogInformation("Unhandled webhook event type: {Type}", stripeEvent.Type);
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error: {Message}", ex.Message);
            if (ex.StripeError != null)
            {
                _logger.LogError("Stripe error details: {Details}", ex.StripeError.Message);
            }
            // Log the raw JSON for debugging
            _logger.LogError("Webhook JSON (first 500 chars): {Json}", json.Length > 500 ? json.Substring(0, 500) : json);
            _logger.LogError("Webhook signature (first 50 chars): {Signature}", stripeSignature.Length > 50 ? stripeSignature.Substring(0, 50) : stripeSignature);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error: {Message}", ex.Message);
            _logger.LogError("Exception type: {Type}", ex.GetType().Name);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { error = "Webhook processing failed", details = ex.Message });
        }
    }

    private async Task HandlePaymentSuccess(PaymentIntent paymentIntent)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.StripePaymentIntentId == paymentIntent.Id);

            if (booking == null)
            {
                _logger.LogWarning($"Booking not found for payment intent {paymentIntent.Id}");
                return;
            }

            if (booking.Status == BookingStatus.Paid)
            {
                _logger.LogInformation($"Booking {booking.Id} already marked as paid");
                return;
            }

            // Update booking status
            booking.Status = BookingStatus.Paid;
            booking.UpdatedAt = DateTime.UtcNow;

            // Decrement spots left
            if (booking.Trip != null)
            {
                booking.Trip.SpotsLeft -= booking.SpotsReserved;
                if (booking.Trip.SpotsLeft < 0)
                {
                    booking.Trip.SpotsLeft = 0;
                }
                booking.Trip.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Payment succeeded for booking {booking.Id}, spots decremented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling payment success for {paymentIntent.Id}");
            throw;
        }
    }

    private async Task HandlePaymentFailed(PaymentIntent paymentIntent)
    {
        try
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.StripePaymentIntentId == paymentIntent.Id);

            if (booking != null && booking.Status == BookingStatus.Pending)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Payment failed for booking {booking.Id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling payment failure for {paymentIntent.Id}");
        }
    }
}

