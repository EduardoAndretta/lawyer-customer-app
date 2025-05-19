using LawyerCustomerApp.Domain.Auth.Common.Models;
using LawyerCustomerApp.Domain.Auth.Interfaces.Services;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Auth.Services;

public class Service : IService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository      _repository;
    public Service(IServiceProvider serviceProvider, IRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository      = repository;
    }

    public async Task<Result<AuthenticateInformationDto>> AuthenticateAsync(
        AuthenticateParametersDto parameters,
        Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<AuthenticateParametersDto>>();

        var validationResult = validator.Validate(parameters);

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

            return resultConstructor.Build<AuthenticateInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.AuthenticateAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<AuthenticateInformationDto>().Incorporate(informationResult);

        var information = informationResult.Value;

        var dto = information.ToDto();

        return resultConstructor.Build<AuthenticateInformationDto>(dto);
    }

    public async Task<Result<RefreshInformationDto>> RefreshAsync(
        RefreshParametersDto parameters,
        Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<RefreshParametersDto>>();

        var validationResult = validator.Validate(parameters);

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

            return resultConstructor.Build<RefreshInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.RefreshAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<RefreshInformationDto>().Incorporate(informationResult);

        var information = informationResult.Value;

        var dto = information.ToDto();

        return resultConstructor.Build<RefreshInformationDto>(dto);
    }

    public async Task<Result> InvalidateAsync(
        InvalidateParametersDto parameters,
        Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<InvalidateParametersDto>>();

        var validationResult = validator.Validate(parameters);

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

        var informationResult = await _repository.InvalidateAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }

    public async Task<Result> ValidateAsync(
        ValidateParametersDto parameters,
        Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<ValidateParametersDto>>();

        var validationResult = validator.Validate(parameters);

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

        var informationResult = await _repository.ValidateAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }
}