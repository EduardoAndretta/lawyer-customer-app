using LawyerCustomerApp.Domain.Combo.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Combo.Interfaces.Services;

public interface IRepository
{
    Task<Result<KeyValueInformation<long>>> PermissionsEnabledForGrantCaseAsync(KeyValueParameters parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformation<long>>> PermissionsEnabledForRevokeCaseAsync(KeyValueParameters parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformation<long>>> PermissionsEnabledForGrantUserAsync(KeyValueParameters parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformation<long>>> PermissionsEnabledForRevokeUserAsync(KeyValueParameters parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformation<long>>> AttributesAsync(KeyValueParameters parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformation<long>>> RolesAsync(KeyValueParameters parameters, Contextualizer contextualizer);
}