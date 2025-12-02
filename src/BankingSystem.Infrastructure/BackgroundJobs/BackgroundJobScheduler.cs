namespace BankingSystem.Infrastructure.BackgroundJobs;

using Hangfire;
using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class BackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly ILogger<BackgroundJobScheduler> _logger;

    public BackgroundJobScheduler(ILogger<BackgroundJobScheduler> logger)
    {
        _logger = logger;
    }

    public string EnqueueJob<T>(System.Linq.Expressions.Expression<System.Action<T>> methodCall)
    {
        var jobId = BackgroundJob.Enqueue(methodCall);
        _logger.LogInformation("Job enqueued with ID {JobId}", jobId);
        return jobId;
    }

    public string ScheduleJob<T>(
        System.Linq.Expressions.Expression<System.Action<T>> methodCall,
        TimeSpan delay)
    {
        var jobId = BackgroundJob.Schedule(methodCall, delay);
        _logger.LogInformation("Job scheduled with ID {JobId} for {Delay}", jobId, delay);
        return jobId;
    }

    public string ScheduleRecurringJob<T>(
        string recurringJobId,
        System.Linq.Expressions.Expression<System.Action<T>> methodCall,
        string cronExpression)
    {
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);
        _logger.LogInformation(
            "Recurring job {RecurringJobId} scheduled with cron expression {CronExpression}",
            recurringJobId,
            cronExpression);
        return recurringJobId;
    }

    public void DeleteJob(string jobId)
    {
        BackgroundJob.Delete(jobId);
        _logger.LogInformation("Job {JobId} deleted", jobId);
    }

    public void DeleteRecurringJob(string recurringJobId)
    {
        RecurringJob.RemoveIfExists(recurringJobId);
        _logger.LogInformation("Recurring job {RecurringJobId} deleted", recurringJobId);
    }
}
