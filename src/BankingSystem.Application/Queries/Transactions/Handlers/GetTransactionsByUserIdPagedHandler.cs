using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.DTOs.Transactions;
using BankingSystem.Application.Queries.Transactions;
using BankingSystem.Application.Models;
using BankingSystem.Application.Exceptions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using BankingSystem.Domain.Entities;

namespace BankingSystem.Application.Queries.Transactions.Handlers;

public class GetTransactionsByUserIdPagedHandler : IRequestHandler<GetTransactionsByUserIdPagedQuery, PagedResult<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTransactionsByUserIdPagedHandler> _logger;

    public GetTransactionsByUserIdPagedHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetTransactionsByUserIdPagedHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsByUserIdPagedQuery request, CancellationToken cancellationToken)
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

        // Start with base query
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Account!.UserId == request.UserId);

        // Apply filters
        if (request.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == request.AccountId.Value);
        }

        if (request.TransactionType.HasValue)
        {
            query = query.Where(t => t.TransactionType == request.TransactionType.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= request.ToDate.Value);
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(t => t.Amount.Amount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(t => t.Amount.Amount <= request.MaxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Description!.ToLower().Contains(searchTerm) ||
                t.ReferenceNumber.ToLower().Contains(searchTerm));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        // Apply pagination
        var transactions = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var transactionDtos = _mapper.Map<IEnumerable<TransactionDto>>(transactions);

        _logger.LogInformation(
            "Retrieved page {PageNumber} ({PageSize} items) of {TotalCount} transactions for user {UserId}",
            request.PageNumber,
            request.PageSize,
            totalCount,
            request.UserId);

        return new PagedResult<TransactionDto>(
            transactionDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    private static IQueryable<Transaction> ApplySorting(
        IQueryable<Transaction> query,
        string sortBy,
        bool descending)
    {
        Expression<Func<Transaction, object>> keySelector = sortBy.ToLower() switch
        {
            "amount" => t => t.Amount.Amount,
            "type" => t => t.TransactionType,
            "description" => t => t.Description ?? string.Empty,
            "reference" => t => t.ReferenceNumber,
            _ => t => t.TransactionDate
        };

        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }
}
