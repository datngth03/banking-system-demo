namespace BankingSystem.Application.Queries.Transactions;

using BankingSystem.Application.DTOs.Transactions;
using MediatR;

/// <summary>
/// Query to generate a transaction receipt
/// </summary>
public class GetTransactionReceiptQuery : IRequest<TransactionReceiptDto>
{
    public Guid TransactionId { get; set; }
}
