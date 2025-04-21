using LawyerCustomerApp.Domain.Chat.Interfaces.Services;
using LawyerCustomerApp.Domain.Chat.Models.Common;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace LawyerCustomerApp.Domain.Chat.Services;

public class Service : IService
{
    private readonly IServiceProvider _serviceProvider;
    public Service(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<bool>> GetAsync(GetParametersDto parameters)
    {
        var resultConstructor = new ResultConstructor();

        var validator = _serviceProvider.GetRequiredService<IValidator<GetParametersDto>>();

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