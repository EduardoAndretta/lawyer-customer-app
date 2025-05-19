using LawyerCustomerApp.External.Interfaces;
using Microsoft.AspNetCore.Diagnostics;

using Error   = LawyerCustomerApp.External.Responses.Error;
using Warning = LawyerCustomerApp.External.Responses.Warning;

namespace LawyerCustomerApp.Application.Configuration;

public static class ExceptionConfiguration
{
    public static IApplicationBuilder UseExceptionConfiguration(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(c => c.Run(async context =>
        {
            var exception = context.Features?.Get<IExceptionHandlerPathFeature>()?.Error ?? null!;

            try
            {
                // [Global Exception Handler]
                await HandleExceptionAsync(context, exception);
 
            }
            catch (Exception localException)
            {
                // [Local Exception Handler]
                await CreateResponseNotMappedException(context, localException);

                return;
            }
        }));

        return app;
    }

    private async static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var errorGeneratorService = context.RequestServices.GetRequiredService<IErrorGeneratorService>();

        var response = errorGeneratorService.CreateError(exception);

        await context.Response.WriteAsJsonAsync(response);
    }

    private async static Task CreateResponseNotMappedException(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = 500;

        var dataService = context.Response.HttpContext.RequestServices.GetRequiredService<IDataService>();

        var warningList = dataService.GetData<List<Warning.Models.Response>>("WarningList");

        var detailsError = new
        {
            InnerExceptionMessage = exception?.InnerException?.Message                                 ?? "Empty",
            Source                = exception?.Source                                                  ?? "Empty",
            Target                = exception?.TargetSite?.ToString()                                  ?? "Empty",
            StackTrace            = exception?.StackTrace?.Split(Environment.NewLine).FirstOrDefault() ?? "Empty"
        };
        
        var response = new Error.Models.Response
        {
            Status = "error",
            Error  = warningList.Any()
                ? new Error.Models.Response.ErrorPropertiesWithDetailsAndWarning
                {
                    StatusNumber = 500,
                    Title        = "Exception",
                    Message      = exception?.Message ?? "Empty",
                    Warnings     = warningList,
                    Details      = detailsError
                }
                : new Error.Models.Response.ErrorPropertiesWithDetails
                {
                    StatusNumber = 500,
                    Title        = "Exception",
                    Message      = exception?.Message ?? "Empty",
                    Details      = detailsError
                }
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
