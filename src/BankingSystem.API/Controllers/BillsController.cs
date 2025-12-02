using BankingSystem.Application.Commands.Bills;
using BankingSystem.Application.Queries.Bills;
using BankingSystem.Application.DTOs.Bills;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankingSystem.API.Controllers;

/// <summary>
/// Manages bill payment operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("api")] // Normal rate limiting
public class BillsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BillsController> _logger;

    public BillsController(IMediator mediator, ILogger<BillsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all pending bills for an account
    /// </summary>
    /// <param name="accountId">Account ID to get pending bills for</param>
    /// <returns>List of pending bills</returns>
    /// <response code="200">Returns list of pending bills</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Can only view bills for own accounts</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<BillDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingBills([FromQuery] Guid accountId)
    {
        _logger.LogInformation("Getting pending bills for account {AccountId}", accountId);

        var query = new GetPendingBillsQuery { AccountId = accountId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Pays a bill from an account
    /// </summary>
    /// <param name="id">Bill ID to pay</param>
    /// <param name="command">Payment details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Bill paid successfully</response>
    /// <response code="400">Invalid request data, ID mismatch, or insufficient funds</response>
    /// <response code="404">Bill or account not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Can only pay bills from own accounts</response>
    [HttpPost("{id}/pay")]
    [EnableRateLimiting("sensitive")] // Strict limit for payment operations
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PayBill(Guid id, [FromBody] PayBillCommand command)
    {
        if (id != command.BillId)
            return BadRequest(new { error = "ID mismatch" });

        _logger.LogInformation("Paying bill {BillId} from account {AccountId}", id, command.AccountId);

        await _mediator.Send(command);
        return Ok(new { message = "Bill paid successfully" });
    }
}
