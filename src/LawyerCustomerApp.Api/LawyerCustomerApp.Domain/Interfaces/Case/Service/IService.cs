using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Case.Interfaces.Services;

public interface IService
{
    Task<Result<SearchInformationDto>> SearchAsync(SearchParametersDto parameters, Contextualizer contextualizer);
    Task<Result> RegisterAsync(RegisterParametersDto parameters, Contextualizer contextualizer);
    Task<Result> AssignLawyerAsync(AssignLawyerParametersDto parameters, Contextualizer contextualizer);
}