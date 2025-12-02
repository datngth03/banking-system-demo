namespace BankingSystem.Application.Queries.Bills;

using BankingSystem.Application.DTOs.Bills;
using MediatR;

public class GetPendingBillsQuery : IRequest<IEnumerable<BillDto>>
{
    public Guid AccountId { get; set; }
}
