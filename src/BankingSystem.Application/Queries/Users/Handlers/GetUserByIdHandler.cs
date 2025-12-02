using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.DTOs.Users;
using BankingSystem.Application.Queries.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Queries.Users.Handlers;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUserByIdHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }
}
