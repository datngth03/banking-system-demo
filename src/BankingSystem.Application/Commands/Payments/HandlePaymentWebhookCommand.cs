namespace BankingSystem.Application.Commands.Payments;

using MediatR;

/// <summary>
/// Command to handle Stripe webhook events
/// </summary>
public class HandlePaymentWebhookCommand : IRequest<bool>
{
    /// <summary>
    /// Raw webhook payload from Stripe
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Stripe-Signature header value for verification
    /// </summary>
    public string SignatureHeader { get; set; } = string.Empty;
}
