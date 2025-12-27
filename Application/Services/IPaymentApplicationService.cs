using Travelette.Relay.Application.DTOs;

namespace Travelette.Relay.Application.Services;

public interface IPaymentApplicationService
{
    Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(CreatePaymentIntentDto dto, CancellationToken cancellationToken = default);
    Task<RefundResult> ProcessRefundAsync(RefundBookingDto dto, CancellationToken cancellationToken = default);
    Task<RefundAllResult> RefundAllBookingsForTripAsync(Guid tripId, string? reason = null, CancellationToken cancellationToken = default);
    Task ProcessWebhookEventAsync(string jsonPayload, string signature, CancellationToken cancellationToken = default);
}

public record RefundResult(
    string RefundId,
    decimal Amount,
    int SpotsRefunded,
    int SpotsRemaining,
    bool IsFullRefund
);

public record RefundAllResult(
    int TotalBookingsRefunded,
    int TotalSpotsRefunded,
    decimal TotalAmountRefunded,
    List<RefundResult> IndividualRefunds
);

