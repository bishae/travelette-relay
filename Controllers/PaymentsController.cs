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

    [HttpPost("refund")]
    public async Task<ActionResult> RefundBooking(RefundBookingDto dto)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

            if (booking == null)
            {
                _logger.LogWarning($"Booking not found: {dto.BookingId}");
                return NotFound("Booking not found");
            }

            if (booking.Status != BookingStatus.Paid)
            {
                return BadRequest($"Cannot refund booking with status: {booking.Status}. Only paid bookings can be refunded.");
            }

            if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
            {
                return BadRequest("Booking does not have a valid payment intent ID");
            }

            if (booking.Trip == null)
            {
                return BadRequest("Booking trip information is missing");
            }

            // Calculate refund amount and spots
            decimal refundAmount;
            int spotsToRefund;
            bool isFullRefund;

            if (dto.SpotsToRefund.HasValue)
            {
                // Refund by spots
                spotsToRefund = dto.SpotsToRefund.Value;
                
                if (spotsToRefund <= 0)
                {
                    return BadRequest("Number of spots to refund must be greater than 0");
                }

                if (spotsToRefund > booking.SpotsReserved)
                {
                    return BadRequest($"Cannot refund {spotsToRefund} spots. Only {booking.SpotsReserved} spots are reserved.");
                }

                // Calculate refund amount based on price per spot
                var pricePerSpot = booking.AmountPaid / booking.SpotsReserved;
                refundAmount = pricePerSpot * spotsToRefund;
                isFullRefund = spotsToRefund == booking.SpotsReserved;
            }
            else if (dto.Amount.HasValue)
            {
                // Refund by amount (existing behavior)
                refundAmount = dto.Amount.Value;
                if (refundAmount <= 0 || refundAmount > booking.AmountPaid)
                {
                    return BadRequest($"Refund amount must be between 0 and {booking.AmountPaid}");
                }

                // Calculate spots to refund based on amount
                var pricePerSpot = booking.AmountPaid / booking.SpotsReserved;
                spotsToRefund = (int)Math.Round(refundAmount / pricePerSpot);
                
                // Ensure we don't refund more spots than reserved
                if (spotsToRefund > booking.SpotsReserved)
                {
                    spotsToRefund = booking.SpotsReserved;
                    refundAmount = booking.AmountPaid; // Full refund
                }
                
                isFullRefund = refundAmount == booking.AmountPaid;
            }
            else
            {
                // Full refund (no amount or spots specified)
                refundAmount = booking.AmountPaid;
                spotsToRefund = booking.SpotsReserved;
                isFullRefund = true;
            }

            _logger.LogInformation($"Processing refund for booking {booking.Id}: Spots={spotsToRefund}, Amount={refundAmount}, Reason={dto.Reason}");

            // Create refund in Stripe
            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = booking.StripePaymentIntentId,
                Amount = (long)(refundAmount * 100), // Convert to cents
                Reason = dto.Reason != null ? RefundReasons.RequestedByCustomer : null,
                Metadata = new Dictionary<string, string>
                {
                    { "bookingId", booking.Id.ToString() },
                    { "customerEmail", booking.CustomerEmail },
                    { "customerName", booking.CustomerName },
                    { "spotsRefunded", spotsToRefund.ToString() }
                }
            };

            var refund = await refundService.CreateAsync(refundOptions);

            // Update booking and trip based on refund type
            if (isFullRefund)
            {
                // Full refund: mark as refunded and restore all spots
                booking.Status = BookingStatus.Refunded;
                booking.SpotsReserved = 0;
                booking.Trip.SpotsLeft += spotsToRefund;
                _logger.LogInformation($"Full refund: Restored {spotsToRefund} spots to trip {booking.Trip.Id}");
            }
            else
            {
                // Partial refund: update spots reserved and restore refunded spots
                booking.SpotsReserved -= spotsToRefund;
                booking.Trip.SpotsLeft += spotsToRefund;
                // Status remains Paid (customer still has remaining spots)
                _logger.LogInformation($"Partial refund: Refunded {spotsToRefund} spots, {booking.SpotsReserved} spots remaining. Restored {spotsToRefund} spots to trip {booking.Trip.Id}");
            }
            
            booking.Trip.UpdatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Refund successful for booking {booking.Id}, Stripe refund ID: {refund.Id}");

            return Ok(new
            {
                success = true,
                refundId = refund.Id,
                amount = refundAmount,
                spotsRefunded = spotsToRefund,
                spotsRemaining = booking.SpotsReserved,
                bookingId = booking.Id,
                message = isFullRefund 
                    ? $"Full refund processed successfully. {spotsToRefund} spot(s) refunded." 
                    : $"Partial refund processed successfully. {spotsToRefund} spot(s) refunded, {booking.SpotsReserved} spot(s) remaining."
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error processing refund: {Message}", ex.Message);
            return StatusCode(500, new { error = $"Stripe error: {ex.Message}", details = ex.StripeError?.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error processing refund", details = ex.Message });
        }
    }
}

