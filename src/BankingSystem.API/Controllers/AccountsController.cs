using BankingSystem.Application.Commands.Accounts;
using BankingSystem.Application.Queries.Accounts;
using BankingSystem.Application.DTOs.Accounts;
using BankingSystem.Application.Constants;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BankingSystem.API.Controllers;

/// <summary>
/// Manages bank account operations including creation, deposits, withdrawals, and transfers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")] // Normal rate limiting for general operations
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AccountsController> _logger;
    private readonly IAccountService _accountService;

    public AccountsController(
        IMediator mediator,
        ILogger<AccountsController> logger,
        IAccountService accountService)
    {
        _mediator = mediator;
        _logger = logger;
        _accountService = accountService;
    }

    /// <summary>
    /// Creates a new bank account
    /// </summary>
    /// <param name="command">Account creation details</param>
    /// <returns>The ID of the newly created account</returns>
    /// <response code="201">Account created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - Invalid or missing token</response>
    /// <response code="403">Forbidden - Users can only create accounts for themselves</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand command)
    {
        // Users can only create accounts for themselves, staff can create for anyone
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == Roles.User && command.UserId != userId)
        {
            return Forbid();
        }

        _logger.LogInformation("Creating account for user {UserId}", command.UserId);

        var accountId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAccountDetails), new { id = accountId }, new { id = accountId });
    }

    /// <summary>
    /// Gets all accounts for the currently authenticated user
    /// </summary>
    /// <returns>List of user's accounts</returns>
    /// <response code="200">Returns the list of accounts</response>
    /// <response code="401">Unauthorized - Invalid or missing token</response>
    [HttpGet("my-accounts")]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAccounts()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting accounts for user {UserId}", userId);

        var query = new GetAccountsByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Gets all accounts for a specific user (Staff only)
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <returns>List of user's accounts</returns>
    /// <response code="200">Returns the list of accounts</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Requires staff role</response>
    [HttpGet("user/{userId}")]
    [Authorize(Policy = Policies.RequireStaffRole)]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserAccounts(Guid userId)
    {
        _logger.LogInformation("Getting accounts for user {UserId}", userId);

        var query = new GetAccountsByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Gets detailed information about a specific account
    /// </summary>
    /// <param name="id">The account ID</param>
    /// <returns>Account details</returns>
    /// <response code="200">Returns the account details</response>
    /// <response code="404">Account not found</response>
    /// <response code="403">Forbidden - Users can only view their own accounts</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAccountDetails(Guid id)
    {
        var query = new GetAccountDetailsQuery { AccountId = id };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { message = "Account not found" });

        // Check authorization: Users can only view their own accounts
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == Roles.User && result.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {AccountId} belonging to user {OwnerId}",
                userId, id, result.UserId);
            return Forbid();
        }

        return Ok(result);
    }

    /// <summary>
    /// Deposits money into an account
    /// </summary>
    /// <param name="id">The account ID</param>
    /// <param name="request">Deposit details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Deposit completed successfully</response>
    /// <response code="400">Invalid amount or account state</response>
    /// <response code="403">Forbidden - Users can only deposit to their own accounts</response>
    /// <response code="404">Account not found</response>
    [HttpPost("{id}/deposit")]
    [EnableRateLimiting("sensitive")] // Stricter limit for money operations
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deposit(Guid id, [FromBody] DepositRequest request)
    {
        await ValidateAccountOwnership(id);

        _logger.LogInformation("Deposit {Amount} to account {AccountId}", request.Amount, id);

        var command = new DepositCommand
        {
            AccountId = id,
            Amount = request.Amount,
            Description = request.Description,
            ReferenceNumber = request.ReferenceNumber
        };

        await _mediator.Send(command);
        return Ok(new { message = "Deposit successful" });
    }

    /// <summary>
    /// Withdraws money from an account
    /// </summary>
    /// <param name="id">The account ID</param>
    /// <param name="request">Withdrawal details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Withdrawal completed successfully</response>
    /// <response code="400">Insufficient funds or invalid amount</response>
    /// <response code="403">Forbidden - Users can only withdraw from their own accounts</response>
    /// <response code="404">Account not found</response>
    [HttpPost("{id}/withdraw")]
    [EnableRateLimiting("sensitive")] // Stricter limit for money operations
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawRequest request)
    {
        await ValidateAccountOwnership(id);

        _logger.LogInformation("Withdraw {Amount} from account {AccountId}", request.Amount, id);

        var command = new WithdrawCommand
        {
            AccountId = id,
            Amount = request.Amount,
            Description = request.Description
        };

        await _mediator.Send(command);
        return Ok(new { message = "Withdrawal successful" });
    }

    /// <summary>
    /// Transfers funds between accounts
    /// </summary>
    /// <param name="command">Transfer details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Transfer completed successfully</response>
    /// <response code="400">Insufficient funds, invalid accounts, or same account transfer</response>
    /// <response code="403">Forbidden - Users can only transfer from their own accounts</response>
    /// <response code="404">Source or destination account not found</response>
    [HttpPost("transfer")]
    [EnableRateLimiting("sensitive")] // Stricter limit for money operations
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferFunds([FromBody] TransferFundsCommand command)
    {
        await ValidateAccountOwnership(command.FromAccountId);

        _logger.LogInformation("Transfer from {FromAccountId} to {ToAccountId}, Amount: {Amount}",
            command.FromAccountId, command.ToAccountId, command.Amount);

        await _mediator.Send(command);
        return Ok(new { message = "Transfer completed successfully" });
    }

    /// <summary>
    /// Closes an account (Staff only)
    /// </summary>
    /// <param name="id">The account ID to close</param>
    /// <returns>Success message</returns>
    /// <response code="200">Account closed successfully</response>
    /// <response code="400">Cannot close account with remaining balance</response>
    /// <response code="403">Forbidden - Requires staff role</response>
    /// <response code="404">Account not found</response>
    [HttpPost("{id}/close")]
    [Authorize(Policy = Policies.CanManageAccounts)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseAccount(Guid id)
    {
        _logger.LogInformation("Closing account {AccountId}", id);

        var command = new CloseAccountCommand { AccountId = id };
        await _mediator.Send(command);
        return Ok(new { message = "Account closed successfully" });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim!);
    }

    private async Task ValidateAccountOwnership(Guid accountId)
    {
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        // Staff can operate on any account
        if (userRole != Roles.User)
            return;

        // Regular users can only operate on their own accounts
        var account = await _accountService.GetAccountByIdAsync(accountId);

        if (account == null)
            throw new Application.Exceptions.NotFoundException(
                string.Format(Application.Constants.ValidationMessages.AccountNotFound, accountId));

        var accountUserId = (Guid)((dynamic)account).UserId;
        if (accountUserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access account {AccountId} belonging to user {OwnerId}",
                userId, accountId, accountUserId);
            throw new Application.Exceptions.ForbiddenException(
                "You do not have permission to access this account");
        }
    }
}
