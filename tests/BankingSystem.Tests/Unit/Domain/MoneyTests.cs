using BankingSystem.Domain.ValueObjects;
using FluentAssertions;

namespace BankingSystem.Tests.Unit.Domain;

public class MoneyTests
{
    [Fact]
    public void CreateMoney_WithValidAmount_ShouldSucceed()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_TwoMoneyObjects_ShouldReturnCorrectSum()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Subtract_TwoMoneyObjects_ShouldReturnCorrectDifference()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(30m, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void IsGreaterThan_WhenFirstIsLarger_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");

        // Act
        var result = money1.IsGreaterThan(money2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLessThan_WhenFirstIsSmaller_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(30m, "USD");
        var money2 = new Money(50m, "USD");

        // Act
        var result = money1.IsLessThan(money2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        // Act
        var result = money1.Equals(money2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Add_DifferentCurrencies_ShouldThrowException()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "EUR");

        // Act
        Action act = () => { var result = money1 + money2; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add amounts in different currencies");
    }

    [Fact]
    public void Multiply_ByScalar_ShouldReturnCorrectResult()
    {
        // Arrange
        var money = new Money(100m, "USD");

        // Act
        var result = money * 2.5m;

        // Assert
        result.Amount.Should().Be(250m);
        result.Currency.Should().Be("USD");
    }
}
