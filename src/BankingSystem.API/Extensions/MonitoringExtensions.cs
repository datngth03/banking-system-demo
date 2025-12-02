namespace BankingSystem.API.Extensions;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.Runtime;
using Serilog;
using Serilog.Events;

/// <summary>
/// Extension methods for configuring monitoring, telemetry, and observability
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Adds OpenTelemetry with tracing and metrics
    /// </summary>
    public static IServiceCollection AddBankingSystemTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["ServiceName"] ?? "BankingSystem.API";
        var serviceVersion = configuration["ServiceVersion"] ?? "1.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = configuration["Environment"] ?? "Development",
                    ["deployment.location"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = (httpContext) =>
                    {
                        // Don't trace health checks and swagger
                        var path = httpContext.Request.Path.Value ?? "";
                        return !path.Contains("/health") &&
                               !path.Contains("/swagger");
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                })
                .AddConsoleExporter()) // For development
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("BankingSystem.*")
                .AddPrometheusExporter()
                .AddConsoleExporter()); // For development

        return services;
    }

    /// <summary>
    /// Configures enhanced Serilog with structured logging and sinks
    /// </summary>
    public static IHostBuilder UseBankingSystemLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            var seqUrl = context.Configuration["Logging:Seq:Url"];
            var seqApiKey = context.Configuration["Logging:Seq:ApiKey"];

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "BankingSystem.API")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File(
                    path: "logs/banking-system-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Warning);

            // Add Seq sink if configured
            if (!string.IsNullOrEmpty(seqUrl))
            {
                if (!string.IsNullOrEmpty(seqApiKey))
                {
                    configuration.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
                }
                else
                {
                    configuration.WriteTo.Seq(seqUrl);
                }
            }
        });
    }
}
