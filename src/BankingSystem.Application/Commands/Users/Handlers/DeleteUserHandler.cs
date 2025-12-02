namespace BankingSystem.Application.Commands.Users.Handlers;

using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Users;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public DeleteUserHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        
        if (user == null)
            throw new NotFoundException(string.Format(ValidationMessages.UserNotFound, request.Id));

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
