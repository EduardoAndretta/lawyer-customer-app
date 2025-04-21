using LawyerCustomerApp.Domain.Search.Models.Common;
using LawyerCustomerApp.External.Models;

namespace LawyerCustomerApp.Domain.Search.Interfaces.Services;

public interface IService
{
    Task<Result<bool>> SearchCasesAsync(SearchCasesParametersDto parameters);
    Task<Result<bool>> SearchLawyersAsync(SearchLawyersParametersDto parameters);
}