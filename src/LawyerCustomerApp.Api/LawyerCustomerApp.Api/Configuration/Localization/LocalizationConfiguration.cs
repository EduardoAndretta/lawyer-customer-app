using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace LawyerCustomerApp.Application.Configuration;

public static class LocalizationConfiguration
{
    public static IServiceCollection AddLocalizationConfiguration(this IServiceCollection services)
    {
       services.AddLocalization();

       services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("pt-BR")
            };

            options.DefaultRequestCulture = new RequestCulture("pt-BR");
            options.SupportedCultures = supportedCultures;                
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
            {
                var currentLanguage = context.Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault();
                var defaultLanguage = string.IsNullOrWhiteSpace(currentLanguage) ? "pt-BR" : currentLanguage;
                
                if (!supportedCultures.Any(s => s.Name.Equals(defaultLanguage)))
                    defaultLanguage = "pt-BR";
                return await Task.FromResult(new ProviderCultureResult(defaultLanguage, defaultLanguage));
            }));
        });

        return services;
    }

    public static IApplicationBuilder UseLocalizationConfiguration(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();

        if (options != null)
            app.UseRequestLocalization(options.Value);

        return app;
    }
}
