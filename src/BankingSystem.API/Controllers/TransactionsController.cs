using BankingSystem.Application.Queries.Transactions;
using BankingSystem.Application.DTOs.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankingSystem.API.Controllers;

/// <summary>
/// Manages transaction queries and history
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")] // Normal rate limiting
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IMediator mediator, ILogger<TransactionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all transactions for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of transactions</returns>
    /// <response code="200">Returns transaction list</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserTransactions(Guid userId)
    {
        _logger.LogInformation("Getting transactions for user {UserId}", userId);

        var query = new GetTransactionsByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets paginated and filtered transactions for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="accountId">Filter by account ID (optional)</param>
    /// <param name="transactionType">Filter by transaction type (Deposit, Withdrawal, Transfer)</param>
    /// <param name="fromDate">Start date for filtering (optional)</param>
    /// <param name="toDate">End date for filtering (optional)</param>
    /// <param name="minAmount">Minimum transaction amount (optional)</param>
    /// <param name="maxAmount">Maximum transaction amount (optional)</param>
    /// <param name="searchTerm">Search in description (optional)</param>
    /// <param name="sortBy">Sort field (default: TransactionDate)</param>
    /// <param name="sortDescending">Sort direction (default: true)</param>
    /// <returns>Paginated transaction list</returns>
    /// <response code="200">Returns paginated transactions</response>
    /// <response code="400">Invalid query parameters</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("user/{userId}/paged")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserTransactionsPaged(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? accountId = null,
        [FromQuery] string? transactionType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "TransactionDate",
        [FromQuery] bool sortDescending = true)
    {
        _logger.LogInformation(
            "Getting paged transactions for user {UserId}, page {PageNumber}, size {PageSize}",
            userId,
            pageNumber,
            pageSize);

        var query = new GetTransactionsByUserIdPagedQuery
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            AccountId = accountId,
            FromDate = fromDate,
            ToDate = toDate,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        // Parse transaction type if provided
        if (!string.IsNullOrEmpty(transactionType) &&
            Enum.TryParse<Domain.Enums.TransactionType>(transactionType, true, out var type))
        {
            query.TransactionType = type;
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets transaction receipt details
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <returns>Transaction receipt</returns>
    /// <response code="200">Returns transaction receipt</response>
    /// <response code="404">Transaction not found</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("{id}/receipt")]
    [ProducesResponseType(typeof(TransactionReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactionReceipt(Guid id)
    {
        _logger.LogInformation("Getting receipt for transaction {TransactionId}", id);

        var query = new GetTransactionReceiptQuery { TransactionId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
