namespace BankingSystem.API.Extensions;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.Runtime;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

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
            var environment = context.HostingEnvironment.EnvironmentName;
            var seqUrl = context.Configuration["Logging:Seq:Url"];
            var seqApiKey = context.Configuration["Logging:Seq:ApiKey"];
            var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];

            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "BankingSystem.API")
                .Enrich.WithProperty("Version", context.Configuration["ServiceVersion"] ?? "1.0.0")
                .Enrich.WithProperty("EnvironmentType", environment);

            // Console logging - all environments
            configuration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{EnvironmentType}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: environment == "Production" ? LogEventLevel.Warning : LogEventLevel.Information);

            // File logging - not in Test environment
            if (environment != "Test")
            {
                var logPath = environment == "Production" 
                    ? "/app/logs/banking-system-.log"  // Container path for production
                    : "logs/banking-system-.log";      // Local path for dev/staging

                configuration.WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: environment == "Production" ? 90 : 30,
                    fileSizeLimitBytes: 100_000_000, // 100 MB
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{EnvironmentType}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Warning);
            }

            // Seq logging - only if configured
            if (!string.IsNullOrEmpty(seqUrl))
            {
                try
                {
                    var seqMinLevel = environment == "Production" 
                        ? LogEventLevel.Warning 
                        : LogEventLevel.Information;

                    if (!string.IsNullOrEmpty(seqApiKey))
                    {
                        configuration.WriteTo.Seq(
                            seqUrl, 
                            apiKey: seqApiKey,
                            restrictedToMinimumLevel: seqMinLevel,
                            period: TimeSpan.FromSeconds(2),  // Batch every 2 seconds
                            batchPostingLimit: 100);          // Max 100 events per batch
                    }
                    else
                    {
                        configuration.WriteTo.Seq(
                            seqUrl,
                            restrictedToMinimumLevel: seqMinLevel,
                            period: TimeSpan.FromSeconds(2),
                            batchPostingLimit: 100);
                    }
                }
                catch (Exception ex)
                {
                    // Log configuration error to console but don't crash
                    Console.WriteLine($"Warning: Failed to configure Seq sink: {ex.Message}");
                }
            }

            // Application Insights for Azure (Production/Staging)
            if (!string.IsNullOrEmpty(appInsightsConnectionString) && 
                (environment == "Production" || environment == "Staging"))
            {
                try
                {
                    configuration.WriteTo.ApplicationInsights(
                        appInsightsConnectionString,
                        TelemetryConverter.Traces,
                        restrictedToMinimumLevel: LogEventLevel.Warning);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to configure Application Insights sink: {ex.Message}");
                }
            }

            // Override minimum level for specific namespaces to reduce noise
            configuration
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", 
                    environment == "Development" ? LogEventLevel.Information : LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
        });
    }
}
