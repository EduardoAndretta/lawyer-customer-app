using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.User.Interfaces.Services;

public interface IService
{
    Task<Result<SearchInformationDto>> SearchAsync(SearchParametersDto parameters, Contextualizer contextualizer);
    Task<Result> RegisterAsync(RegisterParametersDto parameters, Contextualizer contextualizer);
    Task<Result> GrantPermissionsAsync(GrantPermissionsParametersDto parameters, Contextualizer contextualizer);
    Task<Result> RevokePermissionsAsync(RevokePermissionsParametersDto parameters, Contextualizer contextualizer);
}