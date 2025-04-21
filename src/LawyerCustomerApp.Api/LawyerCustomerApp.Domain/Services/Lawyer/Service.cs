using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Lawyer.Common.Models;
using LawyerCustomerApp.Domain.Lawyer.Interfaces.Services;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Lawyer.Services;

public class Service : IService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository      _repository;
    public Service(IServiceProvider serviceProvider, IRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository      = repository;
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
}