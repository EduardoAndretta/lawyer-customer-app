using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Permission.Common.Models;
using LawyerCustomerApp.Domain.Permission.Interfaces.Repositories;
using LawyerCustomerApp.Domain.Permission.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Permission.Services;

public class Service : IService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository      _repository;
    public Service(IServiceProvider serviceProvider, IRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository      = repository;
    }

    #region Case

    public async Task<Result<EnlistedPermissionsFromCaseInformationDto>> EnlistPermissionsFromCaseAsync(EnlistPermissionsFromCaseParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<EnlistPermissionsFromCaseParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<EnlistedPermissionsFromCaseInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.EnlistPermissionsFromCaseAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromCaseInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<EnlistedPermissionsFromCaseInformationDto>(informationResult.Value.ToDto());
    }

    public async Task<Result<GlobalPermissionsRelatedWithCaseInformationDto>> GlobalPermissionsRelatedWithCaseAsync(GlobalPermissionsRelatedWithCaseParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<GlobalPermissionsRelatedWithCaseParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<GlobalPermissionsRelatedWithCaseInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.GlobalPermissionsRelatedWithCaseAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<GlobalPermissionsRelatedWithCaseInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<GlobalPermissionsRelatedWithCaseInformationDto>(informationResult.Value.ToDto());
    }

    public async Task<Result<PermissionsRelatedWithCaseInformationDto>> PermissionsRelatedWithCaseAsync(PermissionsRelatedWithCaseParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<PermissionsRelatedWithCaseParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<PermissionsRelatedWithCaseInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.PermissionsRelatedWithCaseAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<PermissionsRelatedWithCaseInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<PermissionsRelatedWithCaseInformationDto>(informationResult.Value.ToDto());
    }

    public async Task<Result> GrantPermissionsToCaseAsync(GrantPermissionsToCaseParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<GrantPermissionsToCaseParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.GrantPermissionsToCaseAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }

    public async Task<Result> RevokePermissionsToCaseAsync(RevokePermissionsToCaseParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<RevokePermissionsToCaseParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.RevokePermissionsToCaseAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }

    #endregion

    #region User

    public async Task<Result<EnlistedPermissionsFromUserInformationDto>> EnlistPermissionsFromUserAsync(EnlistPermissionsFromUserParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<EnlistPermissionsFromUserParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<EnlistedPermissionsFromUserInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.EnlistPermissionsFromUserAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromUserInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<EnlistedPermissionsFromUserInformationDto>(informationResult.Value.ToDto());
    }

    public async Task<Result<GlobalPermissionsRelatedWithUserInformationDto>> GlobalPermissionsRelatedWithUserAsync(GlobalPermissionsRelatedWithUserParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<GlobalPermissionsRelatedWithUserParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<GlobalPermissionsRelatedWithUserInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.GlobalPermissionsRelatedWithUserAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<GlobalPermissionsRelatedWithUserInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<GlobalPermissionsRelatedWithUserInformationDto>(informationResult.Value.ToDto());
    }

    public async Task<Result<PermissionsRelatedWithUserInformationDto>> PermissionsRelatedWithUserAsync(PermissionsRelatedWithUserParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<PermissionsRelatedWithUserParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<PermissionsRelatedWithUserInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.PermissionsRelatedWithUserAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<PermissionsRelatedWithUserInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<PermissionsRelatedWithUserInformationDto>(informationResult.Value.ToDto());
    }

    public async Task<Result> GrantPermissionsToUserAsync(GrantPermissionsToUserParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<GrantPermissionsToUserParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.GrantPermissionsToUserAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }

    public async Task<Result> RevokePermissionsToUserAsync(RevokePermissionsToUserParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<RevokePermissionsToUserParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.RevokePermissionsToUserAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }

    #endregion

    public async Task<Result<SearchEnabledUsersToGrantPermissionsInformationDto>> SearchEnabledUsersToGrantPermissionsAsync(SearchEnabledUsersToGrantPermissionsParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<SearchEnabledUsersToGrantPermissionsParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<SearchEnabledUsersToGrantPermissionsInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.SearchEnabledUsersToGrantPermissionsAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<SearchEnabledUsersToGrantPermissionsInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<SearchEnabledUsersToGrantPermissionsInformationDto>(informationResult.Value.ToDto());
    }

    public async Task<Result<SearchEnabledUsersToRevokePermissionsInformationDto>> SearchEnabledUsersToRevokePermissionsAsync(SearchEnabledUsersToRevokePermissionsParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<SearchEnabledUsersToRevokePermissionsParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(
                new ValidationError()
                {
                    Status     = 400,
                    SourceCode = this.GetType().Name,
                    Details    = new()
                    {
                        Errors = validationResult.Errors.Select(x =>
                        {
                            var parameters = x.CustomState is string[] array 
                                ? array
                                : Array.Empty<string>();

                            return new ValidationError.DetailsVariation.Item
                            {
                                Parameters = parameters,
                                Field      = x.PropertyName,
                                Identity   = x.ErrorMessage
                            };
                        })
                    }
                });

            return resultConstructor.Build<SearchEnabledUsersToRevokePermissionsInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.SearchEnabledUsersToRevokePermissionsAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<SearchEnabledUsersToRevokePermissionsInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<SearchEnabledUsersToRevokePermissionsInformationDto>(informationResult.Value.ToDto());
    }
}