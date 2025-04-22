using LawyerCustomerApp.Application.Configuration.Events;
using LawyerCustomerApp.Application.Configuration.Responses.Error;
using LawyerCustomerApp.External.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LawyerCustomerApp.Application.Configuration;

public static class IdentityConfiguration
{
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new BaseException<NotFoundJwtKeyError>()
            {
                Constructor = new()
                {
                    Status = 500,
                }
            };

        int byteCount = Encoding.UTF8.GetByteCount(jwtKey);

        byte[] keyBytes = new byte[byteCount];

        if (!Encoding.UTF8.TryGetBytes(jwtKey, keyBytes, out var bytesWritten))
            throw new BaseException<NotWrittenBytesJwtKeyError>()
            {
                Constructor = new()
                {
                    Status = 500,
                }
            };

        services.AddScoped<JwtBearerEvents, ValidationEvents>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer("internal-jwt-bearer", options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken            = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
                        ValidateLifetime         = true,
                        ValidateAudience         = false,
                        ValidateIssuer           = false,
                        ClockSkew                = TimeSpan.Zero
                    };
                    options.EventsType = typeof(JwtBearerEvents);
                });

        services.AddAuthorization(options =>
        {
            var internalJwtBearerPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes("internal-jwt-bearer")
                .RequireAuthenticatedUser()
                .Build();

            var administratorInternalJwtBearerPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes("internal-jwt-bearer")
                .RequireAuthenticatedUser()
                .RequireRole("Administrator")
                .Build();

            options.AddPolicy("internal-jwt-bearer", internalJwtBearerPolicy);
            options.AddPolicy("administrator-internal-jwt-bearer", administratorInternalJwtBearerPolicy);
        });

        return services;
    }
}
