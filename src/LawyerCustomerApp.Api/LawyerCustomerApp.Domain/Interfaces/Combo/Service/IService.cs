using LawyerCustomerApp.Domain.Combo.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Combo.Interfaces.Services;

public interface IService
{
    Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForGrantCaseAsync(KeyValueParametersDto parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForRevokeCaseAsync(KeyValueParametersDto parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForGrantUserAsync(KeyValueParametersDto parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformationDto<long>>> PermissionsEnabledForRevokeUserAsync(KeyValueParametersDto parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformationDto<long>>> AttributesAsync(KeyValueParametersDto parameters, Contextualizer contextualizer);
    Task<Result<KeyValueInformationDto<long>>> RolesAsync(KeyValueParametersDto parameters, Contextualizer contextualizer);
}