using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.External.Configuration;

public static class Configuration
{
    public static IServiceCollection AddExternalDependenciesAndConfiguration(this IServiceCollection services)
    {
        #region Initializer

        services.AddScoped<Interfaces.IInitializerService, Initializer.Services.Service>();

        #endregion

        #region Database

        services.AddScoped<Interfaces.IDatabaseService, Database.Services.Service>();

        services.AddKeyedScoped<Database.Interfaces.IService, Database.Services.Sqlite.Service>(
            Database.Common.Models.Keys.Key.CreateKey(Database.Common.Models.Keys.Key.ProviderType.Sqlite).GetIdentifier());

        #endregion

        #region Data

        services.AddScoped<Interfaces.IDataService, Data.Services.Service>();

        #endregion

        #region Responses

        services.AddScoped<Interfaces.IErrorGeneratorService, Responses.Error.Services.Service>();
        services.AddScoped<Interfaces.ISuccessGeneratorService, Responses.Success.Services.Service>();
        services.AddScoped<Interfaces.IWarningGeneratorService, Responses.Warning.Services.Service>();

        #endregion

        #region Jwt

        services.AddSingleton<Interfaces.IJwtService, Jwt.Services.Service>();

        #endregion

        #region Hash

        services.AddSingleton<Interfaces.IHashService, Hash.AesCbcDeterministicEncryption.Services.Service>();

        #endregion

        return services;
    }
}
