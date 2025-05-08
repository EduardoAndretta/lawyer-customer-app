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
        services.AddScoped<IValidator<Case.Common.Models.CountParametersDto>, Case.Validator.CountParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.DetailsParametersDto>, Case.Validator.DetailsParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.RegisterParametersDto>, Case.Validator.RegisterParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.AssignLawyerParametersDto>, Case.Validator.AssignLawyerParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.AssignCustomerParametersDto>, Case.Validator.AssignCustomerParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.EditParametersDto>, Case.Validator.EditParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.GrantPermissionsParametersDto>, Case.Validator.GrantPermissionsParametersDtoValidator>();
        services.AddScoped<IValidator<Case.Common.Models.RevokePermissionsParametersDto>, Case.Validator.RevokePermissionsParametersDtoValidator>();

        #endregion   

        #region User

        services.AddScoped<User.Interfaces.Services.IService, User.Services.Service>();

        services.AddScoped<User.Interfaces.Services.IRepository, User.Repositories.Repository>();

        services.AddScoped<IValidator<User.Common.Models.SearchParametersDto>, User.Validator.SearchParametersDtoValidator>();
        services.AddScoped<IValidator<User.Common.Models.CountParametersDto>, User.Validator.CountParametersDtoValidator>();
        services.AddScoped<IValidator<User.Common.Models.DetailsParametersDto>, User.Validator.DetailsParametersDtoValidator>();
        services.AddScoped<IValidator<User.Common.Models.RegisterParametersDto>, User.Validator.RegisterParametersDtoValidator>();
        services.AddScoped<IValidator<User.Common.Models.EditParametersDto>, User.Validator.EditParametersDtoValidator>();
        services.AddScoped<IValidator<User.Common.Models.GrantPermissionsParametersDto>, User.Validator.GrantPermissionsParametersDtoValidator>();
        services.AddScoped<IValidator<User.Common.Models.RevokePermissionsParametersDto>, User.Validator.RevokePermissionsParametersDtoValidator>();

        #endregion

        #region Combo

        services.AddScoped<Customer.Interfaces.Services.IService, Customer.Services.Service>();

        services.AddScoped<Customer.Interfaces.Services.IRepository, Customer.Repositories.Repository>();

        services.AddScoped<IValidator<Combo.Common.Models.KeyValueParametersDto>, Combo.Validator.KeyValueParametersDtoValidator>();

        #endregion

        #region Customer

        services.AddScoped<Customer.Interfaces.Services.IService, Customer.Services.Service>();

        services.AddScoped<Customer.Interfaces.Services.IRepository, Customer.Repositories.Repository>();

        services.AddScoped<IValidator<Customer.Common.Models.SearchParametersDto>, Customer.Validator.SearchParametersDtoValidator>();
        services.AddScoped<IValidator<Customer.Common.Models.CountParametersDto>, Customer.Validator.CountParametersDtoValidator>();
        services.AddScoped<IValidator<Customer.Common.Models.DetailsParametersDto>, Customer.Validator.DetailsParametersDtoValidator>();
        services.AddScoped<IValidator<Customer.Common.Models.RegisterParametersDto>, Customer.Validator.RegisterParametersDtoValidator>();

        #endregion

        #region Lawyer

        services.AddScoped<Lawyer.Interfaces.Services.IService, Lawyer.Services.Service>();

        services.AddScoped<Lawyer.Interfaces.Services.IRepository, Lawyer.Repositories.Repository>();

        services.AddScoped<IValidator<Lawyer.Common.Models.SearchParametersDto>, Lawyer.Validator.SearchParametersDtoValidator>();
        services.AddScoped<IValidator<Lawyer.Common.Models.CountParametersDto>, Lawyer.Validator.CountParametersDtoValidator>();
        services.AddScoped<IValidator<Lawyer.Common.Models.DetailsParametersDto>, Lawyer.Validator.DetailsParametersDtoValidator>();
        services.AddScoped<IValidator<Lawyer.Common.Models.RegisterParametersDto>, Lawyer.Validator.RegisterParametersDtoValidator>();

        #endregion

        return services;
    }
}
