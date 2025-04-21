using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Case.Interfaces.Services;

public interface IRepository
{
    Task<Result> RegisterAsync(RegisterParameters parameters, Contextualizer contextualizer);
}