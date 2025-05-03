using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.User.Interfaces.Services;

public interface IRepository
{
    Task<Result<SearchInformation>> SearchAsync(SearchParameters parameters, Contextualizer contextualizer);
    Task<Result> RegisterAsync(RegisterParameters parameters, Contextualizer contextualizer);
    Task<Result> GrantPermissionsAsync(GrantPermissionsParameters parameters, Contextualizer contextualizer);
    Task<Result> RevokePermissionsAsync(RevokePermissionsParameters parameters, Contextualizer contextualizer);
}