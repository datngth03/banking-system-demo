namespace BankingSystem.Application.Constants;

/// <summary>
/// Role constants for authorization
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Support = "Support";
    public const string User = "User";
    
    // Combined roles
    public const string AdminOrManager = "Admin,Manager";
    public const string AdminOrSupport = "Admin,Support";
    public const string AllStaff = "Admin,Manager,Support";
}
