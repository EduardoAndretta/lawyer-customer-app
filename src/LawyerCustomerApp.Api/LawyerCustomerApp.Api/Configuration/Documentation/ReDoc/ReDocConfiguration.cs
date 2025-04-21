using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Linq;

namespace LawyerCustomerApp.Application.Configuration;

public static class ReDocConfiguration
{
    public static IApplicationBuilder UseReDocConfiguration(this IApplicationBuilder app)
    {    
        app.UseReDoc(options =>
        {
            options.RoutePrefix   = "redoc";
            options.SpecUrl       = "/swagger/v1/swagger.json";
            options.DocumentTitle = "API Documentation";
        });

        return app;
    }
}
