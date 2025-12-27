namespace Travelette.Relay.Domain.Services;

public interface IPaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken = default);
    Task<RefundResult> CreateRefundAsync(CreateRefundRequest request, CancellationToken cancellationToken = default);
    Task<PaymentWebhookEvent> ParseWebhookEventAsync(string jsonPayload, string signature, CancellationToken cancellationToken = default);
}

public record CreatePaymentIntentRequest(
    Guid TripId,
    int Spots,
    string CustomerEmail,
    string CustomerName,
    decimal Amount,
    string Currency
);

public record PaymentIntentResult(
    string ClientSecret,
    string PaymentIntentId,
    decimal Amount
);

public record CreateRefundRequest(
    string PaymentIntentId,
    decimal Amount,
    string? Reason
);

public record RefundResult(
    string RefundId,
    decimal Amount
);

public record PaymentWebhookEvent(
    string EventType,
    string PaymentIntentId,
    string? CustomerEmail = null
);

