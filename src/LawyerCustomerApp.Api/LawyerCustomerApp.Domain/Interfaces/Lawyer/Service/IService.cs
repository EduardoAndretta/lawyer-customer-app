using LawyerCustomerApp.Domain.Lawyer.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Lawyer.Interfaces.Services;

public interface IService
{
    Task<Result> RegisterAsync(RegisterParametersDto parameters, Contextualizer contextualizer);
}