using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.User.Interfaces.Services;

public interface IService
{
    Task<Result<SearchInformationDto>> SearchAsync(SearchParametersDto parameters, Contextualizer contextualizer);
    Task<Result<CountInformationDto>> CountAsync(CountParametersDto parameters, Contextualizer contextualizer);
    Task<Result<DetailsInformationDto>> DetailsAsync(DetailsParametersDto parameters, Contextualizer contextualizer);
    Task<Result> RegisterAsync(RegisterParametersDto parameters, Contextualizer contextualizer);
    Task<Result> EditAsync(EditParametersDto parameters, Contextualizer contextualizer);
}