using LawyerCustomerApp.Domain.Auth.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Auth.Interfaces.Services;

public interface IService
{
    Task<Result<AuthenticateInformationDto>> AuthenticateAsync(AuthenticateParametersDto parameters, Contextualizer contextualizer);
    Task<Result<RefreshInformationDto>> RefreshAsync(RefreshParametersDto parameters, Contextualizer contextualizer);
    Task<Result> InvalidateAsync(InvalidateParametersDto parameters, Contextualizer contextualizer);
    Task<Result> ValidateAsync(ValidateParametersDto parameters, Contextualizer contextualizer);
}