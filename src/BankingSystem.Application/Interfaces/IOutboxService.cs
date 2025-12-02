namespace BankingSystem.Application.Interfaces;

public interface IOutboxService
{
    Task PublishOutboxMessagesAsync(CancellationToken cancellationToken = default);
}
