using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.DTOs.Accounts;
using BankingSystem.Application.Queries.Accounts;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Queries.Accounts.Handlers;

public class GetAccountDetailsHandler : IRequestHandler<GetAccountDetailsQuery, AccountDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetAccountDetailsHandler(
        IApplicationDbContext context,
        IMapper mapper,
        ICacheService cacheService)
    {
        _context = context;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<AccountDto?> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        // 1. Try to get from cache first
        var cacheKey = $"account:{request.AccountId}";
        var cachedAccount = await _cacheService.GetAsync<AccountDto>(cacheKey, cancellationToken);

        if (cachedAccount != null)
            return cachedAccount;

        // 2. If not in cache, query database with optimized query
        var account = await _context.Accounts
            .AsNoTracking() // Read-only query optimization
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account == null)
            return null;

        var accountDto = _mapper.Map<AccountDto>(account);

        // 3. Store in cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, accountDto, TimeSpan.FromMinutes(5), cancellationToken);

        return accountDto;
    }
}
