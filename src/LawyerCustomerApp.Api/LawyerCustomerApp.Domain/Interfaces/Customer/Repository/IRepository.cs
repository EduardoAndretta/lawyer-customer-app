using LawyerCustomerApp.Domain.Customer.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Customer.Interfaces.Services;

public interface IRepository
{
    Task<Result<SearchInformation>> SearchAsync(SearchParameters parameters, Contextualizer contextualizer);
    Task<Result<CountInformation>> CountAsync(CountParameters parameters, Contextualizer contextualizer);
    Task<Result<DetailsInformation>> DetailsAsync(DetailsParameters parameters, Contextualizer contextualizer);
    Task<Result> RegisterAsync(RegisterParameters parameters, Contextualizer contextualizer);
}