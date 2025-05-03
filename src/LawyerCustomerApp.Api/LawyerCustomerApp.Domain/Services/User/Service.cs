using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.Domain.User.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.User.Services;

public class Service : IService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository      _repository;
    public Service(IServiceProvider serviceProvider, IRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository      = repository;
    }

    public async Task<Result<SearchInformationDto>> SearchAsync(SearchParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<SearchParametersDto>>();

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

            return resultConstructor.Build<SearchInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.SearchAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<SearchInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<SearchInformationDto>(informationResult.Value.ToOrdinary());
    }

    public async Task<Result> RegisterAsync(
        RegisterParametersDto parameters,
        Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<RegisterParametersDto>>();

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

        var registrationResult = await _repository.RegisterAsync(parsedParameters, contextualizer);

        if (registrationResult.IsFinished)
            return resultConstructor.Build().Incorporate(registrationResult);

        return resultConstructor.Build();
    }

    public async Task<Result> GrantPermissionsAsync(GrantPermissionsParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<GrantPermissionsParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(new MultipleValidationError()
            {
                Status     = 400,
                SourceCode = this.GetType().Name,
                Details    = new()
                {
                    Errors = validationResult.Errors.Select(x =>
                    {
                        const int MaxDepth = 30;

                        MultipleValidationError.DetailsVariation.Item HandleNestedErrors(ValidationFailure item, int depth = 0)
                        {
                            if (depth > MaxDepth)
                                throw new Exception("Max recursion depth reached.");

                            return x.CustomState switch
                            {
                                string[] variation => new MultipleValidationError.DetailsVariation.Item
                                {
                                    Parameters = variation,
                                    Field      = x.PropertyName,
                                    Identity   = x.ErrorMessage
                                },

                                Tuple<string, string[], ValidationFailure[]>(var key, var parameters, var errors) variation => new MultipleValidationError.DetailsVariation.ItemWithErrors
                                {
                                    Key        = key,
                                    Parameters = parameters,
                                    Field      = x.PropertyName,
                                    Identity   = x.ErrorMessage,

                                    Errors = errors.Select(x => HandleNestedErrors(x, depth + 1))
                                },

                                _ => new MultipleValidationError.DetailsVariation.Item
                                {
                                    Parameters = Array.Empty<string>(),
                                    Field      = x.PropertyName,
                                    Identity   = x.ErrorMessage
                                }
                            };
                        };

                        return HandleNestedErrors(x);
                    })
                }
            });
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.GrantPermissionsAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }

    public async Task<Result> RevokePermissionsAsync(RevokePermissionsParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<RevokePermissionsParametersDto>>();

        var validationResult = await validator.ValidateAsync(parameters, contextualizer.CancellationToken);

        if (!validationResult.IsValid)
        {
            resultConstructor.SetConstructor(new MultipleValidationError()
            {
                Status     = 400,
                SourceCode = this.GetType().Name,
                Details    = new()
                {
                    Errors = validationResult.Errors.Select(x =>
                    {
                        const int MaxDepth = 30;

                        MultipleValidationError.DetailsVariation.Item HandleNestedErrors(ValidationFailure item, int depth = 0)
                        {
                            if (depth > MaxDepth)
                                throw new Exception("Max recursion depth reached.");

                            return x.CustomState switch
                            {
                                string[] variation => new MultipleValidationError.DetailsVariation.Item
                                {
                                    Parameters = variation,
                                    Field      = x.PropertyName,
                                    Identity   = x.ErrorMessage
                                },

                                Tuple<string, string[], ValidationFailure[]>(var key, var parameters, var errors) variation => new MultipleValidationError.DetailsVariation.ItemWithErrors
                                {
                                    Key        = key,
                                    Parameters = parameters,
                                    Field      = x.PropertyName,
                                    Identity   = x.ErrorMessage,

                                    Errors = errors.Select(x => HandleNestedErrors(x, depth + 1))
                                },

                                _ => new MultipleValidationError.DetailsVariation.Item
                                {
                                    Parameters = Array.Empty<string>(),
                                    Field      = x.PropertyName,
                                    Identity   = x.ErrorMessage
                                }
                            };
                        };

                        return HandleNestedErrors(x);
                    })
                }
            });
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.RevokePermissionsAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }
}