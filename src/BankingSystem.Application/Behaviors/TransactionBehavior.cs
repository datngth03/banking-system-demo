namespace BankingSystem.Application.Behaviors;

using MediatR;
using System.Transactions;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using (var transaction = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled))
        {
            try
            {
                var response = await next();
                transaction.Complete();
                return response;
            }
            catch (Exception)
            {
                transaction.Dispose();
                throw;
            }
        }
    }
}
