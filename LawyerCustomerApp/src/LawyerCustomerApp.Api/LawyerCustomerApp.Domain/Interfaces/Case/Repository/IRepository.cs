using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Case.Interfaces.Services;

public interface IRepository
{
    Task<Result<SearchInformation>> SearchAsync(SearchParameters parameters, Contextualizer contextualizer);
    Task<Result<CountInformation>> CountAsync(CountParameters parameters, Contextualizer contextualizer);
    Task<Result<DetailsInformation>> DetailsAsync(DetailsParameters parameters, Contextualizer contextualizer);
    Task<Result> RegisterAsync(RegisterParameters parameters, Contextualizer contextualizer);
    Task<Result> AssignLawyerAsync(AssignLawyerParameters parameters, Contextualizer contextualizer);
    Task<Result> AssignCustomerAsync(AssignCustomerParameters parameters, Contextualizer contextualizer);
    Task<Result> EditAsync(EditParameters parameters, Contextualizer contextualizer);
}