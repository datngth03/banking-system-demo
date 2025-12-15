namespace BankingSystem.API.Extensions;

using BankingSystem.Application.Extensions;
using BankingSystem.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBankingSystemServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        Microsoft.Extensions.Hosting.IHostEnvironment environment)
    {
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration, environment);
        return services;
    }
}
