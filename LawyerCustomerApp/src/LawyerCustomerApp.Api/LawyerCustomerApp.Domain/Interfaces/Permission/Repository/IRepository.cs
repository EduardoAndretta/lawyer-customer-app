using LawyerCustomerApp.Domain.Permission.Common.Models;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.Domain.Permission.Interfaces.Repositories;

public interface IRepository
{
    #region Case

    Task<Result<EnlistedPermissionsFromCaseInformation>> EnlistPermissionsFromCaseAsync(EnlistPermissionsFromCaseParameters parameters, Contextualizer contextualizer);
    Task<Result<GlobalPermissionsRelatedWithCaseInformation>> GlobalPermissionsRelatedWithCaseAsync(GlobalPermissionsRelatedWithCaseParameters parameters, Contextualizer contextualizer);
    Task<Result<PermissionsRelatedWithCaseInformation>> PermissionsRelatedWithCaseAsync(PermissionsRelatedWithCaseParameters parameters, Contextualizer contextualizer);
    Task<Result> GrantPermissionsToCaseAsync(GrantPermissionsToCaseParameters parameters, Contextualizer contextualizer);
    Task<Result> RevokePermissionsToCaseAsync(RevokePermissionsToCaseParameters parameters, Contextualizer contextualizer);

    #endregion

    #region User

    Task<Result<EnlistedPermissionsFromUserInformation>> EnlistPermissionsFromUserAsync(EnlistPermissionsFromUserParameters parameters, Contextualizer contextualizer);
    Task<Result<GlobalPermissionsRelatedWithUserInformation>> GlobalPermissionsRelatedWithUserAsync(GlobalPermissionsRelatedWithUserParameters parameters, Contextualizer contextualizer);
    Task<Result<PermissionsRelatedWithUserInformation>> PermissionsRelatedWithUserAsync(PermissionsRelatedWithUserParameters parameters, Contextualizer contextualizer);
    Task<Result> GrantPermissionsToUserAsync(GrantPermissionsToUserParameters parameters, Contextualizer contextualizer);
    Task<Result> RevokePermissionsToUserAsync(RevokePermissionsToUserParameters parameters, Contextualizer contextualizer);

    #endregion

    Task<Result<SearchEnabledUsersToGrantPermissionsInformation>> SearchEnabledUsersToGrantPermissionsAsync(SearchEnabledUsersToGrantPermissionsParameters parameters, Contextualizer contextualizer);
    Task<Result<SearchEnabledUsersToRevokePermissionsInformation>> SearchEnabledUsersToRevokePermissionsAsync(SearchEnabledUsersToRevokePermissionsParameters parameters, Contextualizer contextualizer);
}