using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.Domain.Case.Interfaces.Services;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Case.Services;

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

            return resultConstructor.Build<SearchInformationDto>();
        }

        var parsedParameters = parameters.ToOrdinary();

        var informationResult = await _repository.SearchAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build<SearchInformationDto>().Incorporate(informationResult);

        return resultConstructor.Build<SearchInformationDto>(informationResult.Value.ToOrdinary());
    }

    public async Task<Result> RegisterAsync(RegisterParametersDto parameters, Contextualizer contextualizer)
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

        var informationResult = await _repository.RegisterAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }

    public async Task<Result> AssignLawyerAsync(AssignLawyerParametersDto parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<AssignLawyerParametersDto>>();

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

        var informationResult = await _repository.AssignLawyerAsync(parsedParameters, contextualizer);

        if (informationResult.IsFinished)
            return resultConstructor.Build().Incorporate(informationResult);

        return resultConstructor.Build();
    }
}