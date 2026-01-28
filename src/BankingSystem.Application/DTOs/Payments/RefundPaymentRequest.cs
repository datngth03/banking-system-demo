namespace BankingSystem.Application.DTOs.Payments;

/// <summary>
/// Request DTO for refunding a payment
/// </summary>
public class RefundPaymentRequest
{
    /// <summary>
    /// Transaction ID to refund
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Optional refund reason (e.g., "Customer requested", "Duplicate charge")
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional amount to refund (partial refund)
    /// If null, full refund is issued
    /// </summary>
    public long? Amount { get; set; }
}
