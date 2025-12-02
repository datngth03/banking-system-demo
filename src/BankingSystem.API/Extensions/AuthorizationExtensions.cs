namespace BankingSystem.API.Extensions;

using BankingSystem.Application.Constants;
using Microsoft.AspNetCore.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddBankingSystemAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Role-based policies
            options.AddPolicy(Policies.RequireAdminRole, policy =>
                policy.RequireRole(Roles.Admin));

            options.AddPolicy(Policies.RequireManagerRole, policy =>
                policy.RequireRole(Roles.Manager));

            options.AddPolicy(Policies.RequireSupportRole, policy =>
                policy.RequireRole(Roles.Support));

            options.AddPolicy(Policies.RequireUserRole, policy =>
                policy.RequireRole(Roles.User));

            // Combined role policies
            options.AddPolicy(Policies.RequireAdminOrManager, policy =>
                policy.RequireRole(Roles.Admin, Roles.Manager));

            options.AddPolicy(Policies.RequireStaffRole, policy =>
                policy.RequireRole(Roles.Admin, Roles.Manager, Roles.Support));

            // Feature-based policies
            options.AddPolicy(Policies.CanManageUsers, policy =>
                policy.RequireRole(Roles.Admin, Roles.Manager));

            options.AddPolicy(Policies.CanManageAccounts, policy =>
                policy.RequireRole(Roles.Admin, Roles.Manager, Roles.Support));

            options.AddPolicy(Policies.CanViewReports, policy =>
                policy.RequireRole(Roles.Admin, Roles.Manager));

            options.AddPolicy(Policies.CanManageTransactions, policy =>
                policy.RequireRole(Roles.Admin, Roles.Support));
        });

        return services;
    }
}
