namespace BankingSystem.API.Extensions;

using BankingSystem.API.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseBankingSystemDefaults(this WebApplication app)
    {
        // Security Headers - Must be first to ensure all responses have headers
        app.UseSecurityHeaders();
        
        // Global Exception Handler
        app.UseGlobalExceptionHandler();
        
        app.UseHttpsRedirection();
        app.UseRouting();
        app.MapControllers();
        
        return app;
    }
}
