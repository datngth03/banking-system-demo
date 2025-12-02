using BankingSystem.Application.Commands.Cards;
using BankingSystem.Application.Queries.Cards;
using BankingSystem.Application.DTOs.Cards;
using BankingSystem.Application.Constants;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankingSystem.API.Controllers;

/// <summary>
/// Manages card operations including issuance, activation, and blocking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CardsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CardsController> _logger;
    private readonly IAccountService _accountService;

    public CardsController(
        IMediator mediator,
        ILogger<CardsController> logger,
        IAccountService accountService)
    {
        _mediator = mediator;
        _logger = logger;
        _accountService = accountService;
    }

    /// <summary>
    /// Gets all cards for the currently authenticated user
    /// </summary>
    /// <returns>List of user's cards with masked card numbers</returns>
    /// <response code="200">Returns the list of cards</response>
    /// <response code="401">Unauthorized - Invalid or missing token</response>
    [HttpGet("my-cards")]
    [ProducesResponseType(typeof(IEnumerable<CardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyCards()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting cards for user {UserId}", userId);

        var query = new GetCardsByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Gets all cards for a specific account
    /// </summary>
    /// <param name="accountId">The account ID</param>
    /// <returns>List of cards associated with the account</returns>
    /// <response code="200">Returns the list of cards</response>
    /// <response code="403">Forbidden - Users can only view cards for their own accounts</response>
    /// <response code="404">Account not found</response>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(IEnumerable<CardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCardsByAccount(Guid accountId)
    {
        await ValidateAccountOwnership(accountId);

        _logger.LogInformation("Getting cards for account {AccountId}", accountId);

        var query = new GetCardsByAccountIdQuery { AccountId = accountId };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Issues a new card for an account
    /// </summary>
    /// <param name="command">Card issuance details</param>
    /// <returns>The ID of the newly issued card</returns>
    /// <response code="201">Card issued successfully</response>
    /// <response code="400">Invalid card type or account state</response>
    /// <response code="403">Forbidden - Users can only issue cards for their own accounts</response>
    /// <response code="404">Account not found</response>
    [HttpPost("issue")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IssueCard([FromBody] IssueCardCommand command)
    {
        await ValidateAccountOwnership(command.AccountId);

        _logger.LogInformation(
            "Issuing {CardType} card for account {AccountId}",
            command.CardType,
            command.AccountId);

        var cardId = await _mediator.Send(command);
        return CreatedAtAction(nameof(IssueCard), new { id = cardId }, new { id = cardId });
    }

    /// <summary>
    /// Activates a card
    /// </summary>
    /// <param name="id">The card ID</param>
    /// <param name="request">Activation details including last four digits for verification</param>
    /// <returns>Success message</returns>
    /// <response code="200">Card activated successfully</response>
    /// <response code="400">Invalid last four digits or card already activated</response>
    /// <response code="404">Card not found</response>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateCard(Guid id, [FromBody] ActivateCardRequest request)
    {
        _logger.LogInformation("Activating card {CardId}", id);

        var command = new ActivateCardCommand
        {
            CardId = id,
            LastFourDigits = request.LastFourDigits
        };

        await _mediator.Send(command);
        return Ok(new { message = "Card activated successfully" });
    }

    /// <summary>
    /// Blocks a card
    /// </summary>
    /// <param name="id">The card ID</param>
    /// <param name="request">Block details including optional reason</param>
    /// <returns>Success message</returns>
    /// <response code="200">Card blocked successfully</response>
    /// <response code="400">Card already blocked or invalid state</response>
    /// <response code="404">Card not found</response>
    [HttpPost("{id}/block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockCard(Guid id, [FromBody] BlockCardRequest request)
    {
        _logger.LogInformation("Blocking card {CardId}", id);

        var command = new BlockCardCommand
        {
            CardId = id,
            Reason = request.Reason
        };

        await _mediator.Send(command);
        return Ok(new { message = "Card blocked successfully" });
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
