using Microsoft.AspNetCore.Mvc;
using Travelette.Relay.Application.DTOs;
using Travelette.Relay.Application.Services;

namespace Travelette.Relay.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentApplicationService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentApplicationService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("create-intent")]
    public async Task<ActionResult<PaymentIntentResponseDto>> CreatePaymentIntent(CreatePaymentIntentDto dto)
    {
        try
        {
            var result = await _paymentService.CreatePaymentIntentAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument creating payment intent");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating payment intent");
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ex.Message);
            }
            return BadRequest(ex.Message);
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
        
        if (string.IsNullOrWhiteSpace(stripeSignature))
        {
            _logger.LogWarning("Missing Stripe-Signature header");
            return BadRequest("Missing Stripe-Signature header");
        }

        string json;
        try
        {
            Request.Body.Position = 0;
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
            await _paymentService.ProcessWebhookEventAsync(json, stripeSignature);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument processing webhook: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation processing webhook: {Message}", ex.Message);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error: {Message}", ex.Message);
            _logger.LogError("Exception type: {Type}", ex.GetType().Name);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { error = "Webhook processing failed", details = ex.Message });
        }
    }

    [HttpPost("refund")]
    public async Task<ActionResult> RefundBooking(RefundBookingDto dto)
    {
        try
        {
            var result = await _paymentService.ProcessRefundAsync(dto);
            
            return Ok(new
            {
                success = true,
                refundId = result.RefundId,
                amount = result.Amount,
                spotsRefunded = result.SpotsRefunded,
                spotsRemaining = result.SpotsRemaining,
                bookingId = dto.BookingId,
                message = result.IsFullRefund 
                    ? $"Full refund processed successfully. {result.SpotsRefunded} spot(s) refunded." 
                    : $"Partial refund processed successfully. {result.SpotsRefunded} spot(s) refunded, {result.SpotsRemaining} spot(s) remaining."
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument processing refund");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation processing refund");
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ex.Message);
            }
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund: {Message}", ex.Message);
            return StatusCode(500, new { error = "Error processing refund", details = ex.Message });
        }
    }
}

