using LawyerCustomerApp.Domain.Permission.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Permission.Interfaces.Services;

public interface IService
{
    #region Case

    Task<Result<EnlistedPermissionsFromCaseInformationDto>> EnlistPermissionsFromCaseAsync(EnlistPermissionsFromCaseParametersDto parameters, Contextualizer contextualizer);
    Task<Result<GlobalPermissionsRelatedWithCaseInformationDto>> GlobalPermissionsRelatedWithCaseAsync(GlobalPermissionsRelatedWithCaseParametersDto parameters, Contextualizer contextualizer);
    Task<Result<PermissionsRelatedWithCaseInformationDto>> PermissionsRelatedWithCaseAsync(PermissionsRelatedWithCaseParametersDto parameters, Contextualizer contextualizer);
    Task<Result> GrantPermissionsToCaseAsync(GrantPermissionsToCaseParametersDto parameters, Contextualizer contextualizer);
    Task<Result> RevokePermissionsToCaseAsync(RevokePermissionsToCaseParametersDto parameters, Contextualizer contextualizer);

    #endregion

    #region User

    Task<Result<EnlistedPermissionsFromUserInformationDto>> EnlistPermissionsFromUserAsync(EnlistPermissionsFromUserParametersDto parameters, Contextualizer contextualizer);
    Task<Result<GlobalPermissionsRelatedWithUserInformationDto>> GlobalPermissionsRelatedWithUserAsync(GlobalPermissionsRelatedWithUserParametersDto parameters, Contextualizer contextualizer);
    Task<Result<PermissionsRelatedWithUserInformationDto>> PermissionsRelatedWithUserAsync(PermissionsRelatedWithUserParametersDto parameters, Contextualizer contextualizer);
    Task<Result> GrantPermissionsToUserAsync(GrantPermissionsToUserParametersDto parameters, Contextualizer contextualizer);
    Task<Result> RevokePermissionsToUserAsync(RevokePermissionsToUserParametersDto parameters, Contextualizer contextualizer);

    #endregion

    Task<Result<SearchEnabledUsersToGrantPermissionsInformationDto>> SearchEnabledUsersToGrantPermissionsAsync(SearchEnabledUsersToGrantPermissionsParametersDto parameters, Contextualizer contextualizer);
    Task<Result<SearchEnabledUsersToRevokePermissionsInformationDto>> SearchEnabledUsersToRevokePermissionsAsync(SearchEnabledUsersToRevokePermissionsParametersDto parameters, Contextualizer contextualizer);
}