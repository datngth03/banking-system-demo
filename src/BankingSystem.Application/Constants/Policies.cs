namespace BankingSystem.Application.Constants;

/// <summary>
/// Policy constants for authorization
/// </summary>
public static class Policies
{
    // Role-based policies
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireManagerRole = "RequireManagerRole";
    public const string RequireSupportRole = "RequireSupportRole";
    public const string RequireUserRole = "RequireUserRole";
    
    // Combined policies
    public const string RequireAdminOrManager = "RequireAdminOrManager";
    public const string RequireStaffRole = "RequireStaffRole";
    
    // Feature-based policies
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageAccounts = "CanManageAccounts";
    public const string CanViewReports = "CanViewReports";
    public const string CanManageTransactions = "CanManageTransactions";
}
