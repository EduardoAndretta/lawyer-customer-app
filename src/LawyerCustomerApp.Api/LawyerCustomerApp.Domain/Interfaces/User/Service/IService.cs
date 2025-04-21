using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.User.Interfaces.Services;

public interface IService
{
    Task<Result> RegisterAsync(RegisterParametersDto parameters, Contextualizer contextualizer);
}