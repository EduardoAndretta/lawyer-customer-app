using LawyerCustomerApp.Domain.Customer.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Customer.Interfaces.Services;

public interface IRepository
{
    Task<Result> RegisterAsync(RegisterParameters parameters, Contextualizer contextualizer);
}