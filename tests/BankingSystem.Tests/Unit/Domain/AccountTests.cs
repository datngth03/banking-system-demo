using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using FluentAssertions;

namespace BankingSystem.Tests.Unit.Domain;

public class AccountTests
{
    [Fact]
    public void CreateAccount_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountType = AccountType.Checking;
        var currency = "USD";

        // Act
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountType = accountType,
            AccountNumber = "1234567890",
            Balance = new Money(1000m, currency),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        account.Should().NotBeNull();
        account.UserId.Should().Be(userId);
        account.AccountType.Should().Be(accountType);
        account.Balance.Amount.Should().Be(1000m);
        account.Balance.Currency.Should().Be(currency);
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Account_WithPositiveBalance_ShouldHaveCorrectAmount()
    {
        // Arrange
        var account = CreateTestAccount(1500m);

        // Act & Assert
        account.Balance.Amount.Should().Be(1500m);
        account.Balance.Currency.Should().Be("USD");
    }

    [Fact]
    public void Account_CanBeDeactivated_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var account = CreateTestAccount(0m);
        account.IsActive = true;

        // Act
        account.IsActive = false;

        // Assert
        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Account_WithZeroBalance_ShouldAllowClosure()
    {
        // Arrange
        var account = CreateTestAccount(0m);

        // Act
        account.IsActive = false;
        account.ClosedAt = DateTime.UtcNow;

        // Assert
        account.IsActive.Should().BeFalse();
        account.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Account_ShouldHaveUniqueAccountNumber()
    {
        // Arrange & Act
        var account1 = CreateTestAccount(100m);
        var account2 = CreateTestAccount(200m);

        // Assert
        account1.AccountNumber.Should().NotBe(account2.AccountNumber);
    }

    [Theory]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.Checking)]
    [InlineData(AccountType.MoneyMarket)]
    public void CreateAccount_WithDifferentTypes_ShouldSucceed(AccountType accountType)
    {
        // Arrange & Act
        var account = CreateTestAccount(1000m, accountType);

        // Assert
        account.AccountType.Should().Be(accountType);
    }

    [Fact]
    public void Account_CanHaveIBANAndBIC()
    {
        // Arrange
        var account = CreateTestAccount(1000m);

        // Act
        account.IBAN = "GB82WEST12345698765432";
        account.BIC = "DEUTDEFF";

        // Assert
        account.IBAN.Should().Be("GB82WEST12345698765432");
        account.BIC.Should().Be("DEUTDEFF");
    }

    [Fact]
    public void Account_ShouldTrackCreationDate()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var account = CreateTestAccount(1000m);
        var afterCreation = DateTime.UtcNow;

        // Assert
        account.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        account.CreatedAt.Should().BeOnOrBefore(afterCreation);
    }

    private Account CreateTestAccount(decimal initialBalance, AccountType type = AccountType.Checking)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AccountType = type,
            AccountNumber = GenerateAccountNumber(),
            Balance = new Money(initialBalance, "USD"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private string GenerateAccountNumber()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
    }
}
