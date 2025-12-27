using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Travelette.Relay.Domain.Services;

namespace Travelette.Relay.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IConfiguration configuration,
        ILogger<StripePaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var stripeKey = _configuration["Stripe:SecretKey"];
        if (!string.IsNullOrWhiteSpace(stripeKey))
        {
            StripeConfiguration.ApiKey = stripeKey;
        }
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken cancellationToken = default)
    {
        var stripeKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeKey))
        {
            _logger.LogError("Stripe SecretKey is not configured");
            throw new InvalidOperationException("Payment service is not configured");
        }

        var amountInCents = (long)(request.Amount * 100);
        if (amountInCents < 50)
        {
            throw new ArgumentException("Total amount must be at least $0.50");
        }

        _logger.LogInformation($"Creating payment intent: TripId={request.TripId}, Spots={request.Spots}, Amount={request.Amount}, AmountInCents={amountInCents}");

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = request.Currency,
            Metadata = new Dictionary<string, string>
            {
                { "tripId", request.TripId.ToString() },
                { "spots", request.Spots.ToString() },
                { "customerEmail", request.CustomerEmail },
                { "customerName", request.CustomerName }
            },
            ReceiptEmail = request.CustomerEmail,
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return new PaymentIntentResult(
            paymentIntent.ClientSecret ?? string.Empty,
            paymentIntent.Id,
            request.Amount
        );
    }

    public async Task<RefundResult> CreateRefundAsync(CreateRefundRequest request, CancellationToken cancellationToken = default)
    {
        var refundService = new RefundService();
        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = request.PaymentIntentId,
            Amount = (long)(request.Amount * 100),
            Reason = request.Reason != null ? RefundReasons.RequestedByCustomer : null
        };

        var refund = await refundService.CreateAsync(refundOptions, cancellationToken: cancellationToken);

        return new RefundResult(refund.Id, request.Amount);
    }

    public Task<PaymentWebhookEvent> ParseWebhookEventAsync(string jsonPayload, string signature, CancellationToken cancellationToken = default)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogError("Webhook secret is not configured");
            throw new InvalidOperationException("Webhook secret not configured");
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Missing Stripe-Signature header");
            throw new ArgumentException("Missing Stripe-Signature header");
        }

        _logger.LogInformation("Processing webhook event. Signature: {Signature}", signature.Substring(0, Math.Min(20, signature.Length)));

        var stripeEvent = EventUtility.ConstructEvent(
            jsonPayload,
            signature,
            webhookSecret
        );

        _logger.LogInformation("Webhook event type: {Type}, ID: {Id}", stripeEvent.Type, stripeEvent.Id);

        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        var paymentIntentId = paymentIntent?.Id ?? string.Empty;
        var customerEmail = paymentIntent?.ReceiptEmail;

        return Task.FromResult(new PaymentWebhookEvent(
            stripeEvent.Type,
            paymentIntentId,
            customerEmail
        ));
    }
}

