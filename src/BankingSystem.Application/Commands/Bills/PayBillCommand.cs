namespace BankingSystem.Application.Commands.Bills;

using MediatR;

public class PayBillCommand : IRequest<Unit>
{
    public Guid BillId { get; set; }
    public Guid AccountId { get; set; }
}
