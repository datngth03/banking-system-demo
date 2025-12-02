namespace BankingSystem.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<T> GetRepository<T>() where T : class, IEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<bool> CommitAsync(CancellationToken cancellationToken = default);

    Task<bool> RollbackAsync(CancellationToken cancellationToken = default);
}
