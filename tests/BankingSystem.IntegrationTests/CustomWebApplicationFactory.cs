using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BankingSystem.IntegrationTests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BankingSystemDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using in-memory database for testing
            services.AddDbContext<BankingSystemDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Remove health checks that depend on external services
            var healthCheckDescriptors = services.Where(d => 
                d.ServiceType == typeof(HealthCheckService) || 
                d.ImplementationType?.Namespace?.Contains("HealthChecks") == true).ToList();
            
            foreach (var healthCheckDescriptor in healthCheckDescriptors)
            {
                services.Remove(healthCheckDescriptor);
            }

            // Add simple health check for tests
            services.AddHealthChecks();

            // Build service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<BankingSystemDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}
