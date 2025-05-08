using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Combo.Common.Models;
using LawyerCustomerApp.Domain.Combo.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Combo.Services;

public class Service : IService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository      _repository;
    public Service(IServiceProvider serviceProvider, IRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository      = repository;
    }

    public async Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForGrantCaseAsync(KeyValueParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<KeyValueParametersDto>>();

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

            return resultConstructor.Build<KeyValueInformationDto<long>>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.PermissionsEnabledForGrantCaseAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<KeyValueInformationDto<long>>().Incorporate(informationResult);

        return resultConstructor.Build<KeyValueInformationDto<long>>(informationResult.Value.ToDto());
    }

    public async Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForRevokeCaseAsync(KeyValueParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<KeyValueParametersDto>>();

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

            return resultConstructor.Build<KeyValueInformationDto<long>>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.PermissionsEnabledForRevokeCaseAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<KeyValueInformationDto<long>>().Incorporate(informationResult);

        return resultConstructor.Build<KeyValueInformationDto<long>>(informationResult.Value.ToDto());
    }

    public async Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForGrantUserAsync(KeyValueParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<KeyValueParametersDto>>();

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

            return resultConstructor.Build<KeyValueInformationDto<long>>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.PermissionsEnabledForGrantUserAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<KeyValueInformationDto<long>>().Incorporate(informationResult);

        return resultConstructor.Build<KeyValueInformationDto<long>>(informationResult.Value.ToDto());
    }

    public async Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForRevokeUserAsync(KeyValueParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<KeyValueParametersDto>>();

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

            return resultConstructor.Build<KeyValueInformationDto<long>>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.PermissionsEnabledForRevokeUserAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<KeyValueInformationDto<long>>().Incorporate(informationResult);

        return resultConstructor.Build<KeyValueInformationDto<long>>(informationResult.Value.ToDto());
    }

    public async Task<Result<KeyValueInformationDto<long>>> AttributesAsync(KeyValueParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<KeyValueParametersDto>>();

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

            return resultConstructor.Build<KeyValueInformationDto<long>>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.AttributesAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<KeyValueInformationDto<long>>().Incorporate(informationResult);

        return resultConstructor.Build<KeyValueInformationDto<long>>(informationResult.Value.ToDto());
    }

    public async Task<Result<KeyValueInformationDto<long>>> RolesAsync(KeyValueParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<KeyValueParametersDto>>();

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

            return resultConstructor.Build<KeyValueInformationDto<long>>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.RolesAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<KeyValueInformationDto<long>>().Incorporate(informationResult);

        return resultConstructor.Build<KeyValueInformationDto<long>>(informationResult.Value.ToDto());
    }
}