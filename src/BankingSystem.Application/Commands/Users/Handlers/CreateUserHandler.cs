using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.DTOs.Users;
using BankingSystem.Application.Commands.Users;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Commands.Users.Handlers;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateUserHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
            throw new ValidationFailureException(ValidationMessages.EmailAlreadyRegistered);

        var address = !string.IsNullOrEmpty(request.Street) && !string.IsNullOrEmpty(request.City)
            ? new Address(
                request.Street,
                request.City,
                request.State ?? string.Empty,
                request.PostalCode ?? string.Empty,
                request.Country ?? string.Empty)
            : null;

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = address,
            DateOfBirth = request.DateOfBirth,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
