using Microsoft.Extensions.Options;

namespace LawyerCustomerApp.Application.Configuration
{
    public static class CorsConfiguration
    {
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                var cors           = configuration.GetSection("Cors");
                var allowedOrigins = cors.GetSection("AllowedOrigins").Get<string[]>();

                options.AddPolicy(name: "default",
                    policy =>
                    {
                        if (allowedOrigins != null && allowedOrigins.Any())
                        {
                            policy.WithOrigins(allowedOrigins)
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                        }
                        else
                        {
                            Console.WriteLine("Warning: CORS AllowedOrigins not configured. Allowing any origin.");
                            policy.AllowAnyOrigin()
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                        }
                    });
            });

            return services;
        }

        public static IApplicationBuilder UserCorsConfiguration(this IApplicationBuilder app)
        {
            app.UseCors("default");

            return app;
        }
    }
}
