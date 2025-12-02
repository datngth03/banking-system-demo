using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Monitoring;
using BankingSystem.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace BankingSystem.Tests.Unit.Services;

public class MetricsServiceTests
{
    private readonly MetricsService _metricsService;
    private readonly BankingSystemMetrics _metrics;


    public MetricsServiceTests()
    {
        _metrics = new BankingSystemMetrics();
        _metricsService = new MetricsService(_metrics);
    }

    [Fact]
    public void RecordTransaction_ShouldNotThrowException()
    {
        // Arrange
        var transactionType = "Deposit";
        var amount = 100m;
        var isSuccess = true;

        // Act
        Action act = () => _metricsService.RecordTransaction(transactionType, amount, isSuccess);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordAccountOperation_ShouldNotThrowException()
    {
        // Arrange
        var operationType = "CreateAccount";
        var isSuccess = true;

        // Act
        Action act = () => _metricsService.RecordAccountOperation(operationType, isSuccess);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordAuthentication_WithSuccess_ShouldNotThrowException()
    {
        // Arrange
        var isSuccess = true;

        // Act
        Action act = () => _metricsService.RecordAuthentication(isSuccess);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordAuthentication_WithFailure_ShouldNotThrowException()
    {
        // Arrange
        var isSuccess = false;
        var failureReason = "Invalid credentials";

        // Act
        Action act = () => _metricsService.RecordAuthentication(isSuccess, failureReason);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordApiDuration_ShouldNotThrowException()
    {
        // Arrange
        var endpoint = "/api/accounts";
        var durationMs = 150.5;

        // Act
        Action act = () => _metricsService.RecordApiDuration(endpoint, durationMs);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementActiveUsers_ShouldNotThrowException()
    {
        // Act
        Action act = () => _metricsService.IncrementActiveUsers();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void DecrementActiveUsers_ShouldNotThrowException()
    {
        // Act
        Action act = () => _metricsService.DecrementActiveUsers();

        // Assert
        act.Should().NotThrow();
    }
}
