using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using BankingSystem.API.Extensions;
using BankingSystem.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog with enhanced logging
builder.Host.UseBankingSystemLogging();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add BankingSystem services
builder.Services.AddBankingSystemServices(builder.Configuration, builder.Environment);
builder.Services.AddBankingSystemSwagger();
builder.Services.AddBankingSystemCors(builder.Configuration);
builder.Services.AddBankingSystemHealthChecks(builder.Configuration);
builder.Services.AddBankingSystemRateLimiting(builder.Configuration);

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add Monitoring & Telemetry
builder.Services.AddBankingSystemTelemetry(builder.Configuration);

// Add JWT Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization with custom policies
builder.Services.AddBankingSystemAuthorization();

var app = builder.Build();

// Apply database migrations automatically (only in non-Testing environments)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying database migrations...");

            var context = services.GetRequiredService<BankingSystem.Infrastructure.Persistence.BankingSystemDbContext>();

            // Apply pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                logger.LogInformation("Pending migrations found. Applying...");
                context.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                logger.LogInformation("Database is up to date");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
            // Don't throw - let app continue, migrations can be applied manually
        }
    }
}

// Configure the HTTP request pipeline
app.UseBankingSystemDefaults();

// Response Compression (early in pipeline)
app.UseResponseCompression();

// Swagger (available in all environments for testing purposes)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BankingSystem API v1");
    options.RoutePrefix = "swagger";
});

// Correlation ID (must be early in pipeline)
app.UseCorrelationId();

// Enable CORS
app.UseCors("DefaultCorsPolicy");

// Rate Limiting
app.UseRateLimiter();

// Input Sanitization (before request processing)
app.UseInputSanitization();

// After Swagger & Static Files 
app.UseRequestResponseLogging();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard & Recurring Jobs (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHangfireConfiguration();
}

// Map health checks with detailed response
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = BankingSystem.API.Middleware.HealthCheckResponseWriter.WriteResponse
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = BankingSystem.API.Middleware.HealthCheckResponseWriter.WriteResponse
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = BankingSystem.API.Middleware.HealthCheckResponseWriter.WriteResponse
});

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.Run();

// Expose Program class for integration tests (required by WebApplicationFactory)
public partial class Program { }