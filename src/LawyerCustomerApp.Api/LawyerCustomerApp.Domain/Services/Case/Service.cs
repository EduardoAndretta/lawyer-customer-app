using LawyerCustomerApp.Domain.Case.Interfaces.Services;
using LawyerCustomerApp.Domain.Case.Models.Common;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Case.Services;

public class Service : IService
{
    private readonly IServiceProvider _serviceProvider;
    public Service(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<bool>> CreateAsync(CreateParametersDto parameters)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<CreateParametersDto>>();

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

            return resultConstructor.Build<bool>();
        }
        return resultConstructor.Build<bool>(true);
    }

    public async Task<Result<bool>> DeleteAsync(DeleteParametersDto parameters)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<DeleteParametersDto>>();

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

            return resultConstructor.Build<bool>();
        }
        return resultConstructor.Build<bool>(true);
    }
}