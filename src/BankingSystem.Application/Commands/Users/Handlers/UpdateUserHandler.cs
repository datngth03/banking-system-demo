using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Users;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Commands.Users.Handlers;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
            throw new NotFoundException(string.Format(ValidationMessages.UserNotFound, request.Id));

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Street) && !string.IsNullOrEmpty(request.City))
        {
            user.Address = new Address(
                request.Street,
                request.City,
                request.State ?? string.Empty,
                request.PostalCode ?? string.Empty,
                request.Country ?? string.Empty);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
