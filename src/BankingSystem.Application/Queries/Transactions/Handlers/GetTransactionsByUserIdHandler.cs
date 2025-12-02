using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.DTOs.Transactions;
using BankingSystem.Application.Queries.Transactions;
using BankingSystem.Application.Exceptions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Queries.Transactions.Handlers;

public class GetTransactionsByUserIdHandler : IRequestHandler<GetTransactionsByUserIdQuery, IEnumerable<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTransactionsByUserIdHandler> _logger;

    public GetTransactionsByUserIdHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetTransactionsByUserIdHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionsByUserIdQuery request, CancellationToken cancellationToken)
    {
        // Authorization: Users can only view their own transactions, staff can view any
        if (!_currentUserService.IsStaff && request.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {CurrentUserId} attempted to view transactions of user {RequestedUserId}",
                _currentUserService.UserId,
                request.UserId);

            throw new ForbiddenException("You can only view your own transactions");
        }

        var transactions = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Account!.UserId == request.UserId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} transactions for user {UserId}",
            transactions.Count,
            request.UserId);

        return _mapper.Map<IEnumerable<TransactionDto>>(transactions);
    }
}
