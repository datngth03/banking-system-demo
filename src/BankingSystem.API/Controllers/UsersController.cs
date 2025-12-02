using BankingSystem.Application.Commands.Users;
using BankingSystem.Application.Queries.Users;
using BankingSystem.Application.Constants;
using BankingSystem.Application.DTOs.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BankingSystem.API.Controllers;

/// <summary>
/// Manages user administration operations (Admin/Manager only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[EnableRateLimiting("admin")] // Strict rate limiting for admin operations
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new user (Admin/Manager only)
    /// </summary>
    /// <param name="command">User creation details</param>
    /// <returns>Created user details</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Requires Admin or Manager role</response>
    [HttpPost]
    [Authorize(Policy = Policies.CanManageUsers)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        _logger.LogInformation("Creating user with email {Email}", command.Email);
        
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Gets user details by ID (Staff only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    /// <response code="200">Returns user details</response>
    /// <response code="404">User not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Requires Staff role</response>
    [HttpGet("{id}")]
    [Authorize(Policy = Policies.RequireStaffRole)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var query = new GetUserByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { message = "User not found" });

        return Ok(result);
    }

    /// <summary>
    /// Updates user information (Admin/Manager only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="command">Updated user details</param>
    /// <returns>Success message</returns>
    /// <response code="200">User updated successfully</response>
    /// <response code="400">Invalid request data or ID mismatch</response>
    /// <response code="404">User not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Requires Admin or Manager role</response>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanManageUsers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { error = "ID mismatch" });

        _logger.LogInformation("Updating user {UserId}", id);
        
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a user (Admin only)
    /// </summary>
    /// <param name="id">User ID to delete</param>
    /// <returns>Success message</returns>
    /// <response code="200">User deleted successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Requires Admin role</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdminRole)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("Deleting user {UserId}", id);
        
        var command = new DeleteUserCommand { Id = id };
        await _mediator.Send(command);
        return Ok(new { message = "User deleted successfully" });
    }

    /// <summary>
    /// Unlocks a locked user account (Admin/Manager only)
    /// </summary>
    /// <param name="id">User ID to unlock</param>
    /// <returns>Success message</returns>
    /// <response code="200">Account unlocked successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Requires Admin or Manager role</response>
    [HttpPost("{id}/unlock")]
    [Authorize(Policy = Policies.CanManageUsers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockAccount(Guid id)
    {
        var adminUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        
        _logger.LogInformation("Admin {AdminUserId} attempting to unlock user {UserId}", adminUserId, id);
        
        var command = new UnlockAccountCommand
        {
            UserId = id,
            AdminUserId = adminUserId
        };

        await _mediator.Send(command);
        
        return Ok(new { message = "Account unlocked successfully" });
    }
}
