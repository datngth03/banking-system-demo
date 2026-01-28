namespace BankingSystem.API.Controllers;

using BankingSystem.Application.Commands.Payments;
using BankingSystem.Application.DTOs.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

/// <summary>
/// Payment operations controller
/// Handles card payments, refunds, and payment webhooks
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Charge a card directly
    /// </summary>
    /// <param name="request">Card charge request with payment method and amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response with status and client secret if needed</returns>
    /// <response code="200">Charge initiated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="422">Payment processing failed</response>
    [HttpPost("charge")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChargeCard(
        [FromBody] ChargeCardRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /charge - Amount: {Amount}, Currency: {Currency}", request.Amount, request.Currency);

        var command = new ChargeCardCommand
        {
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethodId = request.PaymentMethodId,
            Description = request.Description,
            Metadata = request.Metadata,
            ReceiptEmail = request.ReceiptEmail,
            ConfirmationToken = request.ConfirmationToken,
            AutoConfirm = request.AutoConfirm
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.Status == "failed")
        {
            _logger.LogWarning("Charge failed: {Error}", result.ErrorMessage);
            return UnprocessableEntity(result);
        }

        _logger.LogInformation("Charge initiated: TransactionId={TransactionId}", result.TransactionId);
        return Ok(result);
    }

    /// <summary>
    /// Pay a bill with a card
    /// </summary>
    /// <param name="request">Bill payment request with bill ID and payment method</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response with transaction details</returns>
    /// <response code="200">Bill payment initiated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Bill not found</response>
    /// <response code="422">Payment processing failed</response>
    [HttpPost("pay-bill")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PayBillWithCard(
        [FromBody] PayBillWithCardRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /pay-bill - BillId: {BillId}", request.BillId);

        var command = new PayBillWithCardCommand
        {
            BillId = request.BillId,
            PaymentMethodId = request.PaymentMethodId,
            Metadata = request.Metadata,
            ReceiptEmail = request.ReceiptEmail
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.ErrorMessage?.Contains("not found") ?? false)
        {
            _logger.LogWarning("Bill not found: BillId={BillId}", request.BillId);
            return NotFound(result);
        }

        if (result.Status == "failed")
        {
            _logger.LogWarning("Bill payment failed: {Error}", result.ErrorMessage);
            return UnprocessableEntity(result);
        }

        _logger.LogInformation("Bill payment initiated: TransactionId={TransactionId}", result.TransactionId);
        return Ok(result);
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    /// <param name="transactionId">Transaction ID to refund</param>
    /// <param name="request">Refund request with optional amount and reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund response with status</returns>
    /// <response code="200">Refund processed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Transaction not found</response>
    /// <response code="422">Refund processing failed</response>
    [HttpPost("{transactionId:guid}/refund")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RefundPayment(
        [FromRoute] Guid transactionId,
        [FromBody] RefundPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /{transactionId}/refund - Amount: {Amount}", transactionId, request.Amount);

        var command = new RefundPaymentCommand
        {
            TransactionId = transactionId,
            Amount = request.Amount,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.ErrorMessage?.Contains("not found") ?? false)
        {
            _logger.LogWarning("Transaction not found: TransactionId={TransactionId}", transactionId);
            return NotFound(result);
        }

        if (result.Status == "failed")
        {
            _logger.LogWarning("Refund failed: {Error}", result.ErrorMessage);
            return UnprocessableEntity(result);
        }

        _logger.LogInformation("Refund processed: TransactionId={TransactionId}, RefundId={RefundId}", transactionId, result.StripePaymentId);
        return Ok(result);
    }

    /// <summary>
    /// Get payment status
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment status and details</returns>
    /// <response code="200">Payment details retrieved</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Transaction not found</response>
    [HttpGet("{transactionId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(
        [FromRoute] Guid transactionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /{transactionId} - Payment status", transactionId);

        // TODO: Implement GetPaymentStatusQuery
        return NotFound("Payment status endpoint not yet fully implemented");
    }

    /// <summary>
    /// Handle Stripe webhook
    /// This endpoint receives events from Stripe when payment status changes
    /// </summary>
    /// <remarks>
    /// Stripe will send POST requests to this endpoint with events like:
    /// - charge.succeeded
    /// - charge.failed
    /// - charge.refunded
    /// - charge.dispute.created
    /// 
    /// The webhook signature is validated automatically.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Webhook processing result</returns>
    /// <response code="200">Webhook processed successfully</response>
    /// <response code="400">Invalid webhook data or signature</response>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /webhook - Stripe webhook received");

        try
        {
            // Read raw request body
            var payload = await ReadBodyAsync(HttpContext.Request);
            var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogWarning("Webhook missing Stripe-Signature header");
                return BadRequest("Missing Stripe-Signature header");
            }

            var command = new HandlePaymentWebhookCommand
            {
                Payload = payload,
                SignatureHeader = signatureHeader
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return BadRequest("Failed to process webhook");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return BadRequest($"Webhook processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to read raw request body
    /// </summary>
    private async Task<string> ReadBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using (var reader = new StreamReader(request.Body, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
    }
}
