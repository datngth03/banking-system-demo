namespace BankingSystem.Application.Queries.Transactions;

using BankingSystem.Application.DTOs.Transactions;
using BankingSystem.Application.Models;
using BankingSystem.Domain.Enums;
using MediatR;

/// <summary>
/// Query to get paginated and filtered transactions for a user
/// </summary>
public class GetTransactionsByUserIdPagedQuery : IRequest<PagedResult<TransactionDto>>
{
    public Guid UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Filtering
    public Guid? AccountId { get; set; }
    public TransactionType? TransactionType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? SearchTerm { get; set; }

    // Sorting
    public string SortBy { get; set; } = "TransactionDate";
    public bool SortDescending { get; set; } = true;
}
