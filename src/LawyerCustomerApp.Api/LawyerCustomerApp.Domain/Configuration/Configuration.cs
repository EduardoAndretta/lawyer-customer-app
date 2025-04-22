using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Configuration;

public static class Configuration
{
    public static IServiceCollection AddDomainDependenciesAndConfiguration(this IServiceCollection services)
    {
        #region Auth

        services.AddScoped<Auth.Interfaces.Services.IService, Auth.Services.Service>();

        services.AddScoped<Auth.Interfaces.Services.IRepository, Auth.Repositories.Repository>();

        services.AddScoped<IValidator<Auth.Common.Models.AuthenticateParametersDto>, Auth.Validator.AuthenticateParametersDtoValidator>();
        services.AddScoped<IValidator<Auth.Common.Models.RefreshParametersDto>, Auth.Validator.RefreshParametersDtoValidator>();
        services.AddScoped<IValidator<Auth.Common.Models.InvalidateParametersDto>, Auth.Validator.InvalidateParametersDtoValidator>();
        services.AddScoped<IValidator<Auth.Common.Models.ValidateParametersDto>, Auth.Validator.ValidateParametersDtoValidator>();

        #endregion

        #region Case

        services.AddScoped<Case.Interfaces.Services.IService, Case.Services.Service>();

        services.AddScoped<Case.Interfaces.Services.IRepository, Case.Repositories.Repository>();

        services.AddScoped<IValidator<Case.Common.Models.SearchParametersDto>, Case.Validator.SearchParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.RegisterParametersDto>, Case.Validator.RegisterParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.AssignLawyerParametersDto>, Case.Validator.AssignLawyerParametersDtoValidator>();

        #endregion

        #region Chat

        services.AddScoped<Chat.Interfaces.Services.IService, Chat.Services.Service>();

        services.AddScoped<IValidator<Chat.Models.Common.GetParametersDto>, Chat.Validator.GetParametersDtoValidator>();

        #endregion

        #region Search

        services.AddScoped<Search.Interfaces.Services.IService, Search.Services.Service>();

        services.AddScoped<IValidator<Search.Models.Common.SearchCasesParametersDto>, Search.Validator.SearchCasesParametersDtoValidator>();
        services.AddScoped<IValidator<Search.Models.Common.SearchLawyersParametersDto>, Search.Validator.SearchLawyersParametersDtoValidator>();

        #endregion

        #region User

        services.AddScoped<User.Interfaces.Services.IService, User.Services.Service>();

        services.AddScoped<User.Interfaces.Services.IRepository, User.Repositories.Repository>();

        services.AddScoped<IValidator<User.Common.Models.RegisterParametersDto>, User.Validator.RegisterParametersDtoValidator>();

        #endregion

        #region Customer

        services.AddScoped<Customer.Interfaces.Services.IService, Customer.Services.Service>();

        services.AddScoped<Customer.Interfaces.Services.IRepository, Customer.Repositories.Repository>();

        services.AddScoped<IValidator<Customer.Common.Models.RegisterParametersDto>, Customer.Validator.RegisterParametersDtoValidator>();

        #endregion

        #region Lawyer

        services.AddScoped<Lawyer.Interfaces.Services.IService, Lawyer.Services.Service>();

        services.AddScoped<Lawyer.Interfaces.Services.IRepository, Lawyer.Repositories.Repository>();

        services.AddScoped<IValidator<Lawyer.Common.Models.RegisterParametersDto>, Lawyer.Validator.RegisterParametersDtoValidator>();

        #endregion

        return services;
    }
}
