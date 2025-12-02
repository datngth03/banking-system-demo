using BankingSystem.Application.DTOs.Bills;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Queries.Bills;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Queries.Bills.Handlers;

public class GetPendingBillsHandler : IRequestHandler<GetPendingBillsQuery, IEnumerable<BillDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingBillsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BillDto>> Handle(GetPendingBillsQuery request, CancellationToken cancellationToken)
    {
        var bills = await _context.Bills
            .Where(b => b.AccountId == request.AccountId && !b.IsPaid)
            .Select(b => new BillDto
            {
                Id = b.Id,
                AccountId = b.AccountId,
                BillNumber = b.BillNumber,
                Biller = b.Biller,
                Amount = b.Amount.Amount,
                Currency = b.Amount.Currency,
                DueDate = b.DueDate,
                Description = b.Description,
                CreatedAt = b.CreatedAt,
                IsPaid = b.IsPaid,
                PaidDate = null
            })
            .ToListAsync(cancellationToken);

        return bills;
    }
}
