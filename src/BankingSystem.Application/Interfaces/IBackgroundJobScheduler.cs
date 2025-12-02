namespace BankingSystem.Application.Interfaces;

public interface IBackgroundJobScheduler
{
    string EnqueueJob<T>(System.Linq.Expressions.Expression<System.Action<T>> methodCall);

    string ScheduleJob<T>(
        System.Linq.Expressions.Expression<System.Action<T>> methodCall,
        TimeSpan delay);

    string ScheduleRecurringJob<T>(
        string recurringJobId,
        System.Linq.Expressions.Expression<System.Action<T>> methodCall,
        string cronExpression);

    void DeleteJob(string jobId);

    void DeleteRecurringJob(string recurringJobId);
}
