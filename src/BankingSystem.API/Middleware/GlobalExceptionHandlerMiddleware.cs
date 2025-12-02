namespace BankingSystem.API.Middleware;

using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Interfaces;
using BankingSystem.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, IErrorTrackingService errorTracker)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorTracker);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IErrorTrackingService errorTracker)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        // Track error for monitoring
        var errorContext = new Dictionary<string, object>
        {
            ["Endpoint"] = context.Request.Path.Value ?? "Unknown",
            ["Method"] = context.Request.Method,
            ["QueryString"] = context.Request.QueryString.Value ?? string.Empty,
            ["UserAgent"] = context.Request.Headers["User-Agent"].ToString(),
            ["TraceId"] = context.TraceIdentifier
        };

        var userId = context.User?.FindFirst("sub")?.Value
            ?? context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        await errorTracker.TrackErrorAsync(exception, errorContext, userId);

        var (statusCode, problemDetails) = exception switch
        {
            // Application Layer Exceptions
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails(
                    context,
                    "Not Found",
                    notFoundEx.Message,
                    (int)HttpStatusCode.NotFound,
                    notFoundEx
                )
            ),
            ValidationFailureException validationEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Validation Error",
                    validationEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    validationEx
                )
            ),
            UnauthorizedException unauthorizedEx => (
                HttpStatusCode.Unauthorized,
                CreateProblemDetails(
                    context,
                    "Unauthorized",
                    unauthorizedEx.Message,
                    (int)HttpStatusCode.Unauthorized,
                    unauthorizedEx
                )
            ),
            ForbiddenException forbiddenEx => (
                HttpStatusCode.Forbidden,
                CreateProblemDetails(
                    context,
                    "Forbidden",
                    forbiddenEx.Message,
                    (int)HttpStatusCode.Forbidden,
                    forbiddenEx
                )
            ),
            BankingApplicationException appEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Application Error",
                    appEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    appEx
                )
            ),

            // Domain Layer Exceptions
            InsufficientFundsException insufficientFundsEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Insufficient Funds",
                    insufficientFundsEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    insufficientFundsEx
                )
            ),
            InvalidAccountException invalidAccountEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Invalid Account",
                    invalidAccountEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    invalidAccountEx
                )
            ),
            InvalidCardException invalidCardEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Invalid Card",
                    invalidCardEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    invalidCardEx
                )
            ),
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Domain Error",
                    domainEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    domainEx
                )
            ),

            // System Exceptions (fallback only)
            UnauthorizedAccessException unauthorizedAccessEx => (
                HttpStatusCode.Unauthorized,
                CreateProblemDetails(
                    context,
                    "Unauthorized",
                    "You are not authorized to access this resource.",
                    (int)HttpStatusCode.Unauthorized,
                    unauthorizedAccessEx
                )
            ),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Invalid Argument",
                    argEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    argEx
                )
            ),
            InvalidOperationException invalidOpEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(
                    context,
                    "Invalid Operation",
                    invalidOpEx.Message,
                    (int)HttpStatusCode.BadRequest,
                    invalidOpEx
                )
            ),

            // Default handler for unexpected exceptions
            _ => (
                HttpStatusCode.InternalServerError,
                CreateProblemDetails(
                    context,
                    "Internal Server Error",
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Please try again later.",
                    (int)HttpStatusCode.InternalServerError,
                    exception
                )
            )
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }

    private ProblemDetails CreateProblemDetails(
        HttpContext context,
        string title,
        string detail,
        int status,
        Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };

        // Add TraceId for debugging
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // Add timestamp
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        // Add stack trace in development mode
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().Name,
                    message = exception.InnerException.Message
                };
            }
        }

        return problemDetails;
    }
}
