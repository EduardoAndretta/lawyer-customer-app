using Microsoft.OpenApi.Models;

namespace LawyerCustomerApp.Application.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.UseOneOfForPolymorphism();
            options.CustomSchemaIds(x => x.FullName);

            bool jwtBearerIsConfigured = services.Any(service =>
                service.ServiceType.FullName?.Contains("JwtBearerHandler") == true);

            bool authorizationIsConfigured = services.Any(service =>
                service.ServiceType.Name.Contains("IAuthorizationService"));

            if (jwtBearerIsConfigured && authorizationIsConfigured)
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name         = "Authorization",
                    Type         = SecuritySchemeType.Http,
                    Scheme       = "bearer",
                    BearerFormat = "JWT",
                    In           = ParameterLocation.Header,
                    Description  = "Enter JWT token like: Bearer {your token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            }
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}
