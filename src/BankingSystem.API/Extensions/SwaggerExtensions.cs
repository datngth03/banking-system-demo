namespace BankingSystem.API.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;

public static class SwaggerExtensions
{
    public static IServiceCollection AddBankingSystemSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            // API Versioning support
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BankingSystem API",
                Version = "v1",
                Description = "A comprehensive banking system API built with Clean Architecture and CQRS pattern",
                Contact = new OpenApiContact
                {
                    Name = "Banking System Team",
                    Email = "support@bankingsystem.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Enable XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // JWT Bearer Authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme.
                              Enter 'Bearer' [space] and then your token in the text input below.
                              Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Group endpoints by tags
            c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default" });
            c.DocInclusionPredicate((name, api) => true);
        });

        return services;
    }
}
