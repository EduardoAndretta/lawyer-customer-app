using LawyerCustomerApp.Domain.Chat.Models.Common;
using LawyerCustomerApp.External.Models;

namespace LawyerCustomerApp.Domain.Chat.Interfaces.Services;

public interface IService
{
    Task<Result<bool>> GetAsync(GetParametersDto parameters);
}