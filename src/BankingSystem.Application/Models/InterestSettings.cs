namespace BankingSystem.Application.Models;

/// <summary>
/// Interest calculation settings for different account types
/// </summary>
public class InterestSettings
{
    /// <summary>
    /// Annual interest rate for Savings accounts (e.g., 0.03 = 3%)
    /// </summary>
    public decimal SavingsAnnualRate { get; set; } = 0.03m;

    /// <summary>
    /// Annual interest rate for Checking accounts (typically 0%)
    /// </summary>
    public decimal CheckingAnnualRate { get; set; } = 0.0m;

    /// <summary>
    /// Annual interest rate for Investment accounts
    /// </summary>
    public decimal InvestmentAnnualRate { get; set; } = 0.05m;

    /// <summary>
    /// Minimum balance required to earn interest
    /// </summary>
    public decimal MinimumBalanceForInterest { get; set; } = 100m;

    /// <summary>
    /// How often interest is calculated and compounded
    /// </summary>
    public CompoundingFrequency CompoundingFrequency { get; set; } = CompoundingFrequency.Monthly;
}

public enum CompoundingFrequency
{
    Daily = 365,
    Monthly = 12,
    Quarterly = 4,
    Annually = 1
}
