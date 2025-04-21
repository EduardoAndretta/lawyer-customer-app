using LawyerCustomerApp.Domain.Case.Models.Common;
using LawyerCustomerApp.External.Models;

namespace LawyerCustomerApp.Domain.Case.Interfaces.Services;

public interface IService
{
    Task<Result<bool>> CreateAsync(CreateParametersDto parameters);
    Task<Result<bool>> DeleteAsync(DeleteParametersDto parameters);
}