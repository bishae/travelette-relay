using Microsoft.Extensions.Logging;
using Travelette.Relay.Application.DTOs;
using Travelette.Relay.Domain.Entities;
using Travelette.Relay.Domain.Repositories;
using Travelette.Relay.Domain.Services;

namespace Travelette.Relay.Application.Services;

public class PaymentApplicationService : IPaymentApplicationService
{
    private readonly ITripRepository _tripRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentService _paymentService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<PaymentApplicationService> _logger;

    public PaymentApplicationService(
        ITripRepository tripRepository,
        IBookingRepository bookingRepository,
        IPaymentService paymentService,
        IConfigurationService configurationService,
        ILogger<PaymentApplicationService> logger)
    {
        _tripRepository = tripRepository;
        _bookingRepository = bookingRepository;
        _paymentService = paymentService;
        _configurationService = configurationService;
        _logger = logger;
    }

    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(CreatePaymentIntentDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await _tripRepository.GetByIdAsync(dto.TripId);
        if (trip == null)
        {
            throw new InvalidOperationException($"Trip not found: {dto.TripId}");
        }

        if (string.IsNullOrWhiteSpace(dto.CustomerEmail))
        {
            throw new ArgumentException("Customer email is required", nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            throw new ArgumentException("Customer name is required", nameof(dto));
        }

        if (dto.Spots <= 0)
        {
            throw new ArgumentException("Number of spots must be greater than 0", nameof(dto));
        }

        if (dto.Spots > trip.SpotsLeft)
        {
            throw new InvalidOperationException($"Only {trip.SpotsLeft} spots available");
        }

        var totalAmount = trip.Price * dto.Spots;
        if (totalAmount <= 0)
        {
            throw new InvalidOperationException("Trip price must be greater than 0");
        }

        var paymentResult = await _paymentService.CreatePaymentIntentAsync(
            new CreatePaymentIntentRequest(
                dto.TripId,
                dto.Spots,
                dto.CustomerEmail,
                dto.CustomerName,
                totalAmount,
                _configurationService.GetCurrency().ToLower()
            ),
            cancellationToken);

        // Create booking record
        var booking = new Booking
        {
            TripId = trip.Id,
            StripePaymentIntentId = paymentResult.PaymentIntentId,
            CustomerEmail = dto.CustomerEmail,
            CustomerName = dto.CustomerName,
            SpotsReserved = dto.Spots,
            AmountPaid = totalAmount,
            Status = BookingStatus.Pending
        };

        await _bookingRepository.AddAsync(booking);

        return new PaymentIntentResponseDto
        {
            ClientSecret = paymentResult.ClientSecret,
            PaymentIntentId = paymentResult.PaymentIntentId,
            Amount = totalAmount
        };
    }

    public async Task<RefundResult> ProcessRefundAsync(RefundBookingDto dto, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(dto.BookingId);
        if (booking == null)
        {
            throw new InvalidOperationException($"Booking not found: {dto.BookingId}");
        }

        if (booking.Status != BookingStatus.Paid)
        {
            throw new InvalidOperationException($"Cannot refund booking with status: {booking.Status}. Only paid bookings can be refunded.");
        }

        if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            throw new InvalidOperationException("Booking does not have a valid payment intent ID");
        }

        if (booking.Trip == null)
        {
            throw new InvalidOperationException("Booking trip information is missing");
        }

        decimal refundAmount;
        int spotsToRefund;
        bool isFullRefund;

        if (dto.SpotsToRefund.HasValue)
        {
            spotsToRefund = dto.SpotsToRefund.Value;
            
            if (spotsToRefund <= 0)
            {
                throw new ArgumentException("Number of spots to refund must be greater than 0", nameof(dto));
            }

            if (spotsToRefund > booking.SpotsReserved)
            {
                throw new InvalidOperationException($"Cannot refund {spotsToRefund} spots. Only {booking.SpotsReserved} spots are reserved.");
            }

            var pricePerSpot = booking.AmountPaid / booking.SpotsReserved;
            refundAmount = pricePerSpot * spotsToRefund;
            isFullRefund = spotsToRefund == booking.SpotsReserved;
        }
        else if (dto.Amount.HasValue)
        {
            refundAmount = dto.Amount.Value;
            if (refundAmount <= 0 || refundAmount > booking.AmountPaid)
            {
                throw new ArgumentException($"Refund amount must be between 0 and {booking.AmountPaid}", nameof(dto));
            }

            var pricePerSpot = booking.AmountPaid / booking.SpotsReserved;
            spotsToRefund = (int)Math.Round(refundAmount / pricePerSpot);
            
            if (spotsToRefund > booking.SpotsReserved)
            {
                spotsToRefund = booking.SpotsReserved;
                refundAmount = booking.AmountPaid;
            }
            
            isFullRefund = refundAmount == booking.AmountPaid;
        }
        else
        {
            refundAmount = booking.AmountPaid;
            spotsToRefund = booking.SpotsReserved;
            isFullRefund = true;
        }

        var refundResult = await _paymentService.CreateRefundAsync(
            new CreateRefundRequest(
                booking.StripePaymentIntentId,
                refundAmount,
                dto.Reason
            ),
            cancellationToken);

        // Update booking and trip based on refund type
        if (isFullRefund)
        {
            booking.Status = BookingStatus.Refunded;
            booking.SpotsReserved = 0;
            booking.Trip.SpotsLeft += spotsToRefund;
        }
        else
        {
            booking.SpotsReserved -= spotsToRefund;
            booking.Trip.SpotsLeft += spotsToRefund;
        }
        
        booking.Trip.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(booking);

        return new RefundResult(
            refundResult.RefundId,
            refundAmount,
            spotsToRefund,
            booking.SpotsReserved,
            isFullRefund
        );
    }

    public async Task<RefundAllResult> RefundAllBookingsForTripAsync(Guid tripId, string? reason = null, CancellationToken cancellationToken = default)
    {
        // Verify trip exists
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
        {
            throw new InvalidOperationException($"Trip not found: {tripId}");
        }

        // Get all paid bookings for this trip
        var bookings = await _bookingRepository.GetByTripIdAsync(tripId);
        var paidBookings = bookings.Where(b => b.Status == BookingStatus.Paid).ToList();

        if (!paidBookings.Any())
        {
            return new RefundAllResult(0, 0, 0, new List<RefundResult>());
        }

        var individualRefunds = new List<RefundResult>();
        int totalSpotsRefunded = 0;
        decimal totalAmountRefunded = 0;

        // Refund each booking
        foreach (var booking in paidBookings)
        {
            try
            {
                var refundDto = new RefundBookingDto
                {
                    BookingId = booking.Id,
                    Reason = reason
                };

                var refundResult = await ProcessRefundAsync(refundDto, cancellationToken);
                individualRefunds.Add(refundResult);
                totalSpotsRefunded += refundResult.SpotsRefunded;
                totalAmountRefunded += refundResult.Amount;

                _logger.LogInformation(
                    $"Refunded booking {booking.Id} for trip {tripId}. Amount: {refundResult.Amount}, Spots: {refundResult.SpotsRefunded}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error refunding booking {booking.Id} for trip {tripId}");
                // Continue with other bookings even if one fails
                // The error will be logged but we'll still try to refund the rest
            }
        }

        _logger.LogInformation(
            $"Completed refund all for trip {tripId}. Total bookings refunded: {individualRefunds.Count}, " +
            $"Total spots: {totalSpotsRefunded}, Total amount: {totalAmountRefunded}");

        return new RefundAllResult(
            individualRefunds.Count,
            totalSpotsRefunded,
            totalAmountRefunded,
            individualRefunds
        );
    }

    public async Task ProcessWebhookEventAsync(string jsonPayload, string signature, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await _paymentService.ParseWebhookEventAsync(jsonPayload, signature, cancellationToken);

        if (webhookEvent.EventType == "payment_intent.succeeded")
        {
            await HandlePaymentSuccess(webhookEvent.PaymentIntentId, cancellationToken);
        }
        else if (webhookEvent.EventType == "payment_intent.payment_failed")
        {
            await HandlePaymentFailed(webhookEvent.PaymentIntentId, cancellationToken);
        }
        else if (webhookEvent.EventType == "payment_intent.created")
        {
            _logger.LogInformation("Payment intent created: {Id}", webhookEvent.PaymentIntentId);
        }
        else
        {
            _logger.LogInformation("Unhandled webhook event type: {Type}", webhookEvent.EventType);
        }
    }

    private async Task HandlePaymentSuccess(string paymentIntentId, CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingRepository.GetByStripePaymentIntentIdAsync(paymentIntentId);

            if (booking == null)
            {
                _logger.LogWarning($"Booking not found for payment intent {paymentIntentId}");
                return;
            }

            if (booking.Status == BookingStatus.Paid)
            {
                _logger.LogInformation($"Booking {booking.Id} already marked as paid");
                return;
            }

            booking.Status = BookingStatus.Paid;
            booking.UpdatedAt = DateTime.UtcNow;

            if (booking.Trip != null)
            {
                booking.Trip.SpotsLeft -= booking.SpotsReserved;
                if (booking.Trip.SpotsLeft < 0)
                {
                    booking.Trip.SpotsLeft = 0;
                }
                booking.Trip.UpdatedAt = DateTime.UtcNow;
            }

            await _bookingRepository.UpdateAsync(booking);
            _logger.LogInformation($"Payment succeeded for booking {booking.Id}, spots decremented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling payment success for {paymentIntentId}");
            throw;
        }
    }

    private async Task HandlePaymentFailed(string paymentIntentId, CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingRepository.GetByStripePaymentIntentIdAsync(paymentIntentId);

            if (booking != null && booking.Status == BookingStatus.Pending)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(booking);
                _logger.LogInformation($"Payment failed for booking {booking.Id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling payment failure for {paymentIntentId}");
        }
    }
}

