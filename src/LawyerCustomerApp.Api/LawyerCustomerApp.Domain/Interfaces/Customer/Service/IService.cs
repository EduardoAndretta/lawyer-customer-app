using LawyerCustomerApp.Domain.Customer.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Customer.Interfaces.Services;

public interface IService
{
    Task<Result> RegisterAsync(RegisterParametersDto parameters, Contextualizer contextualizer);
}