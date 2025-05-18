using Dapper;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Permission.Common.Models;
using LawyerCustomerApp.Domain.Permission.Interfaces.Repositories;
using LawyerCustomerApp.Domain.Permission.Repositories.Models;
using LawyerCustomerApp.Domain.Permission.Responses.Repositories.Error;
using LawyerCustomerApp.Domain.Permission.Responses.Repositories.Success;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;

using PermissionSymbols = LawyerCustomerApp.External.Models.Permission.Permissions;

namespace LawyerCustomerApp.Domain.Permission.Repositories;

internal class Repository : IRepository
{
    private readonly IConfiguration _configuration;

    private readonly IDatabaseService _databaseService;
    public Repository(IConfiguration configuration, IDatabaseService databaseService)
    {
        _configuration = configuration;

        _databaseService = databaseService;
    }

    #region Case

    #region EnlistPermissionsFromCaseAsync

    public async Task<Result<EnlistedPermissionsFromCaseInformation>> EnlistPermissionsFromCaseAsync(EnlistPermissionsFromCaseParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>().Incorporate(roleIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>().Incorporate(attributeAccountResult);

        var permission = new
        {
            // [Related to CASES WITH (USER OR ROLE) specific permission assigned]

            ViewCasePermissionId            = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),
            ViewPermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_CASE, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicCasePermissionId         = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),
            ViewOwnCasePermissionId            = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPermissionsOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyCasePermissionId            = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer),
            ViewPermissionsAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_CASE, contextualizer)

        };

        var result = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
        ('HasViewPermissionsAnyCasePermission', @ViewPermissionsAnyCasePermissionId),
        ('HasViewPermissionsOwnCasePermission', @ViewPermissionsOwnCasePermissionId),
        ('HasViewPermissionsCasePermission',    @ViewPermissionsCasePermissionId),
        ('HasViewOwnCasePermission',            @ViewOwnCasePermissionId),
        ('HasViewPublicCasePermission',         @ViewPublicCasePermissionId),
        ('HasViewAnyCasePermission',            @ViewAnyCasePermissionId),
        ('HasViewCasePermission',               @ViewCasePermissionId)
),
[grants] AS (

    -- [user grants]
    SELECT 
        [PC].[permission_name], 
        [PGU].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId

    UNION

    -- [role grants]
    SELECT 
        [PC].[permission_name], 
        [PG].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND
         [PG].[role_id]       = @RoleId

    UNION

    -- [case ACL grants]
    SELECT 
        [PC].[permission_name], 
        [PGC].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_case] [PGC]
      ON [PGC].[permission_id]   = [PC].[permission_id] AND 
         [PGC].[user_id]         = @UserId              AND 
         [PGC].[role_id]         = @RoleId              AND 
         [PGC].[related_case_id] = @RelatedCaseId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsAnyCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPermissionsAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsOwnCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPermissionsOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPermissionsCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'            AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission'         AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'            AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'               AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCasePermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new
            {
                ViewPermissionsAnyCasePermissionId = permission.ViewPermissionsAnyCasePermissionId,
                ViewPermissionsOwnCasePermissionId = permission.ViewPermissionsOwnCasePermissionId,
                ViewPermissionsCasePermissionId    = permission.ViewPermissionsCasePermissionId,

                ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
                ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,
                ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
                ViewCasePermissionId       = permission.ViewCasePermissionId,

                AttributeId   = parameters.AttributeId,
                UserId        = parameters.UserId,
                RelatedCaseId = parameters.RelatedCaseId,
                RoleId        = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.EnlistPermissionsFromCase>(queryPermissions, queryPermissionsParameters);

            // [Case Information]

            const string queryCaseInformations = @"
SELECT 
    [C].[private]                                         AS [Private], 
    (CASE WHEN [C].[user_id] = @UserId THEN 1 ELSE 0 END) AS [Owner]
FROM [cases] [C] WHERE [C].[id] = @RelatedCaseId";

            var queryCaseInformationParameters = new
            {
                RelatedCaseId = parameters.RelatedCaseId,
                UserId        = parameters.UserId
            };

            var userInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner)>(queryCaseInformations, queryCaseInformationParameters);

            // [VIEW]
            if (((userInformationResult.Private.HasValue && userInformationResult.Private.Value) && !permissionsResult.HasViewPublicCasePermission) &&
                ((userInformationResult.Owner.HasValue   && userInformationResult.Owner.Value)   && !permissionsResult.HasViewOwnCasePermission)    &&
                !permissionsResult.HasViewCasePermission &&
                !permissionsResult.HasViewAnyCasePermission)
            {
                resultConstructor.SetConstructor(new UserNotFoundError());

                return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>();
            }

            // [VIEW_PERMISSIONS]
            if (((userInformationResult.Owner.HasValue && userInformationResult.Owner.Value) && !permissionsResult.HasViewPermissionsOwnCasePermission) &&
                !permissionsResult.HasViewPermissionsCasePermission &&
                !permissionsResult.HasViewPermissionsAnyCasePermission)
            {
                resultConstructor.SetConstructor(new EnlistPermissionsFromCaseDeniedError());

                return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>();
            }

            // [Principal Query]

            var queryParameters = new
            {
                UserId = parameters.UserId,
                RelatedCaseId = parameters.RelatedCaseId
            };

            var queryText = $@"
SELECT
   [U].[name] AS [UserName],
   [P].[name] AS [PermissionName],
   [R].[name] AS [RoleName],
   [A].[name] AS [AttributeName],
   [U].[id]   AS [UserId],
   [P].[id]   AS [PermissionId],
   [R].[id]   AS [RoleId],
   [A].[id]   AS [AttributeId]
FROM [permission_grants_case] [PGC]
LEFT JOIN [users]       [U] ON [U].[id] = [PGC].[user_id]
LEFT JOIN [permissions] [P] ON [P].[id] = [PGC].[permission_id]
LEFT JOIN [roles]       [R] ON [R].[id] = [PGC].[role_id]
LEFT JOIN [attributes]  [A] ON [A].[id] = [PGC].[attribute_id]
WHERE [PGC].[related_case_id] = @RelatedCaseId";

            EnlistedPermissionsFromCaseInformation information;

            using (var multiple = await connection.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText: queryText,
                    parameters: queryParameters,
                    transaction: connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout: TimeSpan.FromHours(1).Milliseconds
                    )))
            {
                information = new EnlistedPermissionsFromCaseInformation
                {
                    Items = await multiple.ReadAsync<EnlistedPermissionsFromCaseInformation.ItemProperties>()
                };
            }

            return resultConstructor.Build<EnlistedPermissionsFromCaseInformation>(information);
        });

        return result;
    }

    #endregion

    #region GlobalPermissionsRelatedWithCaseAsync

    public async Task<Result<GlobalPermissionsRelatedWithCaseInformation>> GlobalPermissionsRelatedWithCaseAsync(GlobalPermissionsRelatedWithCaseParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<GlobalPermissionsRelatedWithCaseInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to USER or ROLE permission]

            RegisterCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REGISTER_CASE, contextualizer),

            EditOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_OWN_CASE, contextualizer),

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            ViewPermissionsOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_CASE, contextualizer),

            AssignLawyerOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_LAWYER_OWN_CASE, contextualizer),

            AssignCustomerOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_CUSTOMER_OWN_CASE, contextualizer),

            GrantPermissionsOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_CASE, contextualizer),

            RevokePermissionsOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            EditAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_CASE, contextualizer),

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer),

            ViewPermissionsAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_CASE, contextualizer),

            AssignLawyerAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_LAWYER_ANY_CASE, contextualizer),

            AssignCustomerAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_CUSTOMER_ANY_CASE, contextualizer),

            GrantPermissionsAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_CASE, contextualizer),

            RevokePermissionsAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_CASE, contextualizer)
        };

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<GlobalPermissionsRelatedWithCaseInformation>().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build<GlobalPermissionsRelatedWithCaseInformation>().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<GlobalPermissionsRelatedWithCaseInformation>().Incorporate(roleIdResult);

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
        ('HasRegisterCasePermission',             @RegisterCasePermissionId),
        ('HasEditOwnCasePermission',              @EditOwnCasePermissionId),
        ('HasAnyCasePermission',                  @EditAnyCasePermissionId),
        ('HasViewAnyCasePermission',              @ViewAnyCasePermissionId),
        ('HasViewOwnCasePermission',              @ViewOwnCasePermissionId),
        ('HasViewPublicCasePermission',           @ViewPublicCasePermissionId),
        ('HasViewPermissionsOwnCasePermission',   @ViewPermissionsOwnCasePermissionId),
        ('HasViewPermissionsAnyCasePermission',   @ViewPermissionsAnyCasePermissionId),
        ('HasAssignLawyerOwnCasePermission',      @AssignLawyerOwnCasePermissionId),
        ('HasAssignLawyerAnyCasePermission',      @AssignLawyerAnyCasePermissionId),
        ('HasAssignCustomerOwnCasePermission',    @AssignCustomerOwnCasePermissionId),
        ('HasAssignCustomerAnyCasePermission',    @AssignCustomerAnyCasePermissionId),
        ('HasGrantPermissionsOwnCasePermission',  @GrantPermissionsOwnCasePermissionId),
        ('HasGrantPermissionsAnyCasePermission',  @GrantPermissionsAnyCasePermissionId),
        ('HasRevokePermissionsOwnCasePermission', @RevokePermissionsOwnCasePermissionId),
        ('HasRevokePermissionsAnyCasePermission', @RevokePermissionsAnyCasePermissionId)
),
[grants] AS (
    SELECT
        [PC].[permission_name],
        [PGU].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId
    UNION
    SELECT
        [PC].[permission_name],
        [PG].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND 
         [PG].[role_id]       = @RoleId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasRegisterCasePermission'             AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RegisterCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditOwnCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditOwnCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAnyCasePermission'                  AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [AnyCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewAnyCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewOwnCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission'           AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPublicCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsOwnCasePermission'   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsOwnCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsAnyCasePermission'   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsAnyCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignLawyerOwnCasePermission'      AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [AssignLawyerOwnCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignLawyerAnyCasePermission'      AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [AssignLawyerAnyCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignCustomerOwnCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [AssignCustomerOwnCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignCustomerAnyCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [AssignCustomerAnyCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnCasePermission'  AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsOwnCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyCasePermission'  AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsAnyCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsOwnCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsAnyCase]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name]";

        var queryPermissionsParameters = new
        {
            RegisterCasePermissionId = permission.RegisterCasePermissionId,

            EditOwnCasePermissionId = permission.EditOwnCasePermissionId,
            EditAnyCasePermissionId = permission.EditAnyCasePermissionId,

            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,

            ViewPermissionsOwnCasePermissionId = permission. ViewPermissionsOwnCasePermissionId,
            ViewPermissionsAnyCasePermissionId = permission.ViewPermissionsAnyCasePermissionId,

            AssignLawyerOwnCasePermissionId = permission.AssignLawyerOwnCasePermissionId,
            AssignLawyerAnyCasePermissionId = permission.AssignLawyerAnyCasePermissionId,

            AssignCustomerOwnCasePermissionId = permission.AssignCustomerOwnCasePermissionId,
            AssignCustomerAnyCasePermissionId = permission.AssignCustomerAnyCasePermissionId,

            GrantPermissionsOwnCasePermissionId = permission.GrantPermissionsOwnCasePermissionId,
            GrantPermissionsAnyCasePermissionId = permission.GrantPermissionsAnyCasePermissionId,

            RevokePermissionsOwnCasePermissionId = permission.RevokePermissionsOwnCasePermissionId,
            RevokePermissionsAnyCasePermissionId = permission.RevokePermissionsAnyCasePermissionId,

            UserId        = parameters.UserId,
            AttributeId   = parameters.AttributeId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<GlobalPermissionsRelatedWithCaseInformation>(queryPermissions, queryPermissionsParameters);

        return resultConstructor.Build< GlobalPermissionsRelatedWithCaseInformation>(permissionsResult);
    }

    #endregion

    #region PermissionsRelatedWithCaseAsync

    public async Task<Result<PermissionsRelatedWithCaseInformation>> PermissionsRelatedWithCaseAsync(PermissionsRelatedWithCaseParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<PermissionsRelatedWithCaseInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            EditCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_CASE, contextualizer),

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            ViewPermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_CASE, contextualizer),

            AssignLawyerCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_LAWYER_CASE, contextualizer),

            AssignCustomerCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_CUSTOMER_CASE, contextualizer),

            GrantPermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CASE, contextualizer),

            RevokePermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CASE, contextualizer)
        };

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<PermissionsRelatedWithCaseInformation>().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build<PermissionsRelatedWithCaseInformation>().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<PermissionsRelatedWithCaseInformation>().Incorporate(roleIdResult);

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
        ('HasEditCasePermission',               @EditCasePermissionId),
        ('HasViewCasePermission',               @ViewCasePermissionId),
        ('HasViewPermissionsCasePermission',    @ViewPermissionsCasePermissionId),
        ('HasAssignLawyerCasePermission',       @AssignLawyerCasePermissionId),
        ('HasAssignCustomerCasePermission',     @AssignCustomerCasePermissionId),
        ('HasGrantPermissionsCasePermission',   @GrantPermissionsCasePermissionId),
        ('HasRevokePermissionsCasePermission',  @RevokePermissionsCasePermissionId)
),
[grants] AS (
    SELECT
        [PC].[permission_name],
        [PGC].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_case] [PGC]
      ON [PGC].[permission_id]     = [PC].[permission_id] AND
         [PGC].[user_id]           = @UserId              AND
         [PGC].[role_id]           = @RoleId              AND
         [PGC].[related_case_id]   = @RelatedCaseId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsCasePermission'   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignLawyerCasePermission'      AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [AssignLawyerCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignCustomerCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [AssignCustomerCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsCasePermission'  AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsCase],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsCase]
FROM [permission_checks] [PC]
LEFT JOIN [acl_grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            EditCasePermissionId = permission.EditCasePermissionId,

            ViewCasePermissionId = permission.ViewCasePermissionId,

            ViewPermissionsCasePermissionId = permission.ViewPermissionsCasePermissionId,

            AssignLawyerCasePermissionId = permission.AssignLawyerCasePermissionId,

            AssignCustomerCasePermissionId = permission.AssignCustomerCasePermissionId,

            GrantPermissionsCasePermissionId = permission.GrantPermissionsCasePermissionId,

            RevokePermissionsCasePermissionId = permission.RevokePermissionsCasePermissionId,

            UserId        = parameters.UserId,
            AttributeId   = parameters.AttributeId,
            RoleId        = parameters.RoleId,
            RelatedCaseId = parameters.RelatedCaseId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionsRelatedWithCaseInformation>(queryPermissions, queryPermissionsParameters);

        return resultConstructor.Build<PermissionsRelatedWithCaseInformation>(permissionsResult);
    }

    #endregion

    #region GrantPermissionsToCaseAsync

    public async Task<Result> GrantPermissionsToCaseAsync(GrantPermissionsToCaseParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            GrantPermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CASE, contextualizer),

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            GrantPermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_USER, contextualizer),
            GrantPermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            // [CASE]

            GrantPermissionsOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_CASE, contextualizer),

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            // [USER]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            // [CASE]

            GrantPermissionsAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_CASE, contextualizer),

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer),

            // [USER]

            GrantPermissionsAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_USER, contextualizer),
            GrantPermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            GrantPermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_USER, contextualizer),
            GrantPermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
        };

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(roleIdResult);

        // [Related Case Id]
        var realtedCaseIdResult = await ValidateCaseId(
            parameters.RelatedCaseId,
            contextualizer);

        if (realtedCaseIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(realtedCaseIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build().Incorporate(attributeAccountResult);

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasGrantPermissionsAnyCasePermission',                @GrantPermissionsAnyCasePermissionId),
    ('HasGrantPermissionsOwnCasePermission',                @GrantPermissionsOwnCasePermissionId),
    ('HasGrantPermissionsCasePermission',                   @GrantPermissionsCasePermissionId),
    ('HasViewAnyCasePermission',                            @ViewAnyCasePermissionId),
    ('HasViewOwnCasePermission',                            @ViewOwnCasePermissionId),
    ('HasViewPublicCasePermission',                         @ViewPublicCasePermissionId),
    ('HasViewCasePermission',                               @ViewCasePermissionId),

    ('HasGrantPermissionsAnyUserPermission',                @GrantPermissionsAnyUserPermissionId),
    ('HasGrantPermissionsAnyLawyerAccountUserPermission',   @GrantPermissionsAnyLawyerAccountUserPermissionId),
    ('HasGrantPermissionsAnyCustomerAccountUserPermission', @GrantPermissionsAnyCustomerAccountUserPermissionId),
    ('HasGrantPermissionsOwnUserPermission',                @GrantPermissionsOwnUserPermissionId),
    ('HasGrantPermissionsOwnLawyerAccountUserPermission',   @GrantPermissionsOwnLawyerAccountUserPermissionId),
    ('HasGrantPermissionsOwnCustomerAccountUserPermission', @GrantPermissionsOwnCustomerAccountUserPermissionId),
    ('HasGrantPermissionsUserPermission',                   @GrantPermissionsUserPermissionId),
    ('HasGrantPermissionsLawyerAccountUserPermission',      @GrantPermissionsLawyerAccountUserPermissionId),
    ('HasGrantPermissionsCustomerAccountUserPermission',    @GrantPermissionsCustomerAccountUserPermissionId),
    ('HasViewOwnUserPermission',                            @ViewOwnUserPermissionId),
    ('HasViewOwnLawyerAccountUserPermission',               @ViewOwnLawyerAccountUserPermissionId),
    ('HasViewOwnCustomerAccountUserPermission',             @ViewOwnCustomerAccountUserPermissionId),
    ('HasViewPublicUserPermission',                         @ViewPublicUserPermissionId),
    ('HasViewPublicLawyerAccountUserPermission',            @ViewPublicLawyerAccountUserPermissionId),
    ('HasViewPublicCustomerAccountUserPermission',          @ViewPublicCustomerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                            @ViewAnyUserPermissionId),
    ('HasViewAnyLawyerAccountUserPermission',               @ViewAnyLawyerAccountUserPermissionId),
    ('HasViewAnyCustomerAccountUserPermission',             @ViewAnyCustomerAccountUserPermissionId),
    ('HasViewUserPermission',                               @ViewUserPermissionId),
    ('HasViewLawyerAccountUserPermission',                  @ViewLawyerAccountUserPermissionId),
    ('HasViewCustomerAccountUserPermission',                @ViewCustomerAccountUserPermissionId)
),
[grants] AS (

    -- [user grants]
    SELECT 
        [PC].[permission_name], 
        [PGU].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId

    UNION

    -- [role grants]
    SELECT 
        [PC].[permission_name], 
        [PG].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND
         [PG].[role_id]       = @RoleId

    UNION

    -- [case ACL grants]
    SELECT 
        [PC].[permission_name], 
        [PGC].[attribute_id],
         1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_case] [PGC]
      ON [PGC].[permission_id]   = [PC].[permission_id] AND 
         [PGC].[user_id]         = @UserId              AND 
         [PGC].[role_id]         = @RoleId              AND 
         [PGC].[related_case_id] = @RelatedCaseId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'             AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'             AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission'          AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'                AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCasePermission],
    
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyUserPermission'                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyLawyerAccountUserPermission'   AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyCustomerAccountUserPermission' AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnUserPermission'                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnLawyerAccountUserPermission'   AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnCustomerAccountUserPermission' AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsUserPermission'                   AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsLawyerAccountUserPermission'      AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsCustomerAccountUserPermission'    AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                            AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'               AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'             AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                         AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission'            AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission'          AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                            AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'               AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'             AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUserPermission'                               AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewLawyerAccountUserPermission'                  AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCustomerAccountUserPermission'                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCustomerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            GrantPermissionsAnyCasePermissionId = permission.GrantPermissionsAnyCasePermissionId,
            GrantPermissionsOwnCasePermissionId = permission.GrantPermissionsOwnCasePermissionId,
            GrantPermissionsCasePermissionId    = permission.GrantPermissionsCasePermissionId,

            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,
            ViewCasePermissionId       = permission.ViewCasePermissionId,

            GrantPermissionsAnyUserPermissionId                = permission.GrantPermissionsAnyUserPermissionId,
            GrantPermissionsAnyLawyerAccountUserPermissionId   = permission.GrantPermissionsAnyLawyerAccountUserPermissionId,
            GrantPermissionsAnyCustomerAccountUserPermissionId = permission.GrantPermissionsAnyCustomerAccountUserPermissionId,

            GrantPermissionsOwnUserPermissionId                = permission.GrantPermissionsOwnUserPermissionId,
            GrantPermissionsOwnLawyerAccountUserPermissionId   = permission.GrantPermissionsOwnLawyerAccountUserPermissionId,
            GrantPermissionsOwnCustomerAccountUserPermissionId = permission.GrantPermissionsOwnCustomerAccountUserPermissionId,

            GrantPermissionsUserPermissionId                = permission.GrantPermissionsUserPermissionId,
            GrantPermissionsLawyerAccountUserPermissionId   = permission.GrantPermissionsLawyerAccountUserPermissionId,
            GrantPermissionsCustomerAccountUserPermissionId = permission.GrantPermissionsCustomerAccountUserPermissionId,

            ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,
            ViewOwnLawyerAccountUserPermissionId   = permission.ViewOwnLawyerAccountUserPermissionId,
            ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

            ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,
            ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,
            ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

            ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
            ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
            ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

            ViewUserPermissionId                = permission.ViewUserPermissionId,
            ViewLawyerAccountUserPermissionId   = permission.ViewLawyerAccountUserPermissionId,
            ViewCustomerAccountUserPermissionId = permission.ViewCustomerAccountUserPermissionId,

            UserId        = parameters.UserId,
            RelatedCaseId = parameters.RelatedCaseId,
            AttributeId   = parameters.AttributeId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.GrantPermissionsToCase>(queryPermissions, queryPermissionsParameters);

        // [Case Information]

        const string queryCaseInformations = "SELECT [C].[Private], CASE WHEN [C].[user_id] = @UserId THEN 1 ELSE 0 END AS [Owner] FROM [cases] [C] WHERE [C].[id] = @RelatedCaseId";

        var queryCaseInformationParameters = new
        {
            UserId        = parameters.UserId,
            RelatedCaseId = parameters.RelatedCaseId
        };

        var caseInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner)>(queryCaseInformations, queryCaseInformationParameters);

        // [VIEW]
        if (((caseInformationResult.Private.HasValue && caseInformationResult.Private.Value) && !permissionsResult.HasViewPublicCasePermission) &&
            ((caseInformationResult.Owner.HasValue   && caseInformationResult.Owner.Value)   && !permissionsResult.HasViewOwnCasePermission)    &&
            !permissionsResult.HasViewCasePermission &&
            !permissionsResult.HasViewAnyCasePermission)
        {
            resultConstructor.SetConstructor(new CaseNotFoundError());

            return resultConstructor.Build();
        }

        // [GRANT_PERMISSIONS]
        if (((caseInformationResult.Owner.HasValue && caseInformationResult.Owner.Value) && !permissionsResult.HasGrantPermissionsOwnCasePermission) &&
            !permissionsResult.HasGrantPermissionsCasePermission &&
            !permissionsResult.HasGrantPermissionsAnyCasePermission)
        {
            resultConstructor.SetConstructor(new GrantPermissionsToCaseDeniedError());

            return resultConstructor.Build();
        }

        var distinctPermission = parameters.Permissions.Distinct();

        var internalValues = new InternalValues.GrantPermissionsToCase()
        {
            Data = new()
            {
                Items = distinctPermission
                    .Distinct()
                    .ToDictionary(
                        x => x.Id,
                        x => new InternalValues.GrantPermissionsToCase.DataPropreties.Item()
                        {
                            Id = x.Id,

                            // [From parameters]

                            UserId       = x.UserId,
                            PermissionId = x.PermissionId,
                            RoleId       = x.RoleId,
                            AttributeId  = x.AttributeId
                        })
            }
        };

        // [User Id]
        var userIdResultDictionary = await ValidateUserId(
            internalValues.Data.Items.Values
                .Select(x => x.UserId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!userIdResultDictionary.TryGetValue(item.UserId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Attribute Id]
        var attributeIdResultDictionary = await ValidateAttributeId(
            internalValues.Data.Items.Values
                .Select(x => x.AttributeId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!attributeIdResultDictionary.TryGetValue(item.AttributeId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Role Id]
        var roleIdResultDictionary = await ValidateRoleId(
            internalValues.Data.Items.Values
                .Select(x => x.RoleId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!roleIdResultDictionary.TryGetValue(item.RoleId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Permission Id]
        var permissionIdResultDictionary = await ValidatePermissionId(
            internalValues.Data.Items.Values
                .Select(x => x.PermissionId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!permissionIdResultDictionary.TryGetValue(item.PermissionId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Attribute Account]
        var attributeAccountResultDictionary = await ValidateAttributeAccount(
            internalValues.Data.Items.Values
                .Select(x => (UserId: x.UserId, AttributeId: x.AttributeId))
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!attributeAccountResultDictionary.TryGetValue((UserId: item.UserId, AttributeId: item.AttributeId), out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        const string queryAttributes = "SELECT [A].[id] AS [Id], [A].[name] AS [Name] FROM [Attributes] [A]";

        var attributes = await connection.Connection.QueryAsync<(int Id, string Name)>(queryAttributes);

        const string queryAllowedPermissions = @"
SELECT [P].[id] AS [Id] FROM [Permissions] [P] WHERE [P].[name] IN 
('VIEW_CASE', 'VIEW_PERMISSIONS_CASE', 'ASSIGN_LAWYER_CASE', 'ASSIGN_CUSTOMER_CASE', 'ASSIGN_CUSTOMER_CASE', 'GRANT_PERMISSIONS_CASE', 'REVOKE_PERMISSIONS_CASE')";

        var allowedPermissions = await connection.Connection.QueryAsync<int?>(queryAllowedPermissions);

        foreach (var item in internalValues.Data.Items.Values)
        {
            var resultContructor = new ResultConstructor();

            if (!allowedPermissions.Contains(item.PermissionId))
            {
                resultContructor.SetConstructor(new ForbiddenPermissionsToGrantToCaseError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }

            var hasPermissionToAssignUser = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {                  
                    HasViewOwnUserPermission = permissionsResult.HasViewOwnUserPermission,
                    HasViewAnyUserPermission = permissionsResult.HasViewAnyUserPermission,

                    HasViewPublicUserPermission             = permissionsResult.HasViewPublicUserPermission,
                    HasViewOwnLawyerAccountUserPermission   = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                    HasViewAnyLawyerAccountUserPermission   = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                   
                    HasViewPublicLawyerAccountUserPermission   = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                    HasViewOwnCustomerAccountUserPermission    = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                    HasViewAnyCustomerAccountUserPermission    = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                    HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,
                    
                    HasGrantPermissionsOwnUserPermission = permissionsResult.HasGrantPermissionsOwnUserPermission,
                    HasGrantPermissionsAnyUserPermission = permissionsResult.HasGrantPermissionsAnyUserPermission,

                    HasGrantPermissionsOwnLawyerAccountUserPermission = permissionsResult.HasGrantPermissionsOwnLawyerAccountUserPermission,
                    HasGrantPermissionsAnyLawyerAccountUserPermission = permissionsResult.HasGrantPermissionsAnyLawyerAccountUserPermission,

                    HasGrantPermissionsOwnCustomerAccountUserPermission = permissionsResult.HasGrantPermissionsOwnCustomerAccountUserPermission,
                    HasGrantPermissionsAnyCustomerAccountUserPermission = permissionsResult.HasGrantPermissionsAnyCustomerAccountUserPermission,
 
                    ViewUserPermissionId                = permission.ViewUserPermissionId,
                    ViewLawyerAccountUserPermissionId   = permission.ViewLawyerAccountUserPermissionId,
                    ViewCustomerAccountUserPermissionId = permission.ViewCustomerAccountUserPermissionId,

                    GrantPermissionsUserPermissionId                 = permission.GrantPermissionsUserPermissionId,
                    GrantPermissionsLawyerAccountUserPermissionId    = permission.GrantPermissionsLawyerAccountUserPermissionId,
                    GrantPermissionsCustomerAccountUserPermissionId  = permission.GrantPermissionsCustomerAccountUserPermissionId,

                    AttributeName = attributes.First(x => x.Id == item.AttributeId).Name,

                    AttributeId  = item.AttributeId,
                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId,

                    RelatedCaseId  = parameters.RelatedCaseId,

                    ExternalUserId = parameters.UserId,
                };

                var queryText = @$"
WITH [view_permission] AS (
    SELECT
        CASE
            WHEN

                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasViewOwnUserPermission OR @HasViewAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_USER]

                    ([U].[private] = 1 AND (
                        @ViewUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1 FROM [permission_grants_relationship] [PGRu]
                            WHERE
                                [PGRu].[related_user_id] = @UserId               AND 
                                [PGRu].[user_id]         = @ExternalUserId       AND 
                                [PGRu].[role_id]         = @RoleId               AND 
                                [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                                ([PGRu].[attribute_id] IS NULL OR [PGRu].[attribute_id] = @AttributeId)
                        )
                        OR @HasViewAnyUserPermission = 1
                    ))

                    OR 

                    -- [Layer 3: Public]

                    ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                )

                AND

                -- [Block 2: account-level check (LAWYER vs CUSTOMER)]

                (
                    CASE 
                        WHEN @AttributeName = 'LAWYER' THEN
                            CASE
                                -- [Layer 1: Ownership]

                                WHEN ([L].[user_id] = @ExternalUserId AND (@HasViewOwnLawyerAccountUserPermission OR @HasViewAnyLawyerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_LAWYER_ACCOUNT_USER]

                                WHEN [L].[private] = 1 AND (
                                    @ViewLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRl]
                                        WHERE
                                            [PGRl].[related_user_id] = @UserId                            AND
                                            [PGRl].[user_id]         = @ExternalUserId                    AND
                                            [PGRl].[role_id]         = @RoleId                            AND
                                            [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
                                            ([PGRl].[attribute_id] IS NULL OR [PGRl].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasViewAnyLawyerAccountUserPermission = 1
                                ) THEN 1

                                -- [Layer 3: Public]

                                WHEN ([L].[private] = 0 AND (@HasViewPublicLawyerAccountUserPermission = 1 OR @HasViewAnyLawyerAccountUserPermission = 1)) THEN 1

                                ELSE 0
                            END
                        WHEN @AttributeName = 'CUSTOMER' THEN
                            CASE
                                -- [Layer 1: Ownership]

                                WHEN ([C].[user_id] = @ExternalUserId AND (@HasViewOwnCustomerAccountUserPermission OR @HasViewAnyCustomerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_CUSTOMER_ACCOUNT_USER]

                                WHEN [C].[private] = 1 AND (
                                    @ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRc]
                                        WHERE
                                            [PGRc].[related_user_id] = @UserId                             AND 
                                            [PGRc].[user_id]         = @ExternalUserId                      AND 
                                            [PGRc].[role_id]         = @RoleId                              AND 
                                            [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                            ([PGRc].[attribute_id] IS NULL OR [PGRc].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasViewAnyCustomerAccountUserPermission = 1
                                ) THEN 1

                                -- [Layer 3: Public]

                                WHEN ([C].[private] = 0 AND (@HasViewPublicCustomerAccountUserPermission = 1 OR @HasViewAnyCustomerAccountUserPermission = 1)) THEN 1
                                
                                ELSE 0
                            END
                        ELSE 0
                    END = 1
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customers] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
),
[grant_permission] AS (
    SELECT
        CASE
            WHEN
                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasGrantPermissionsOwnUserPermission OR @HasGrantPermissionsAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSIONS_USER]

                    (@GrantPermissionsUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                        WHERE
                            [PGRu2].[related_user_id] = @UserId                           AND 
                            [PGRu2].[user_id]         = @ExternalUserId                   AND 
                            [PGRu2].[role_id]         = @RoleId                           AND 
                            [PGRu2].[permission_id]   = @GrantPermissionsUserPermissionId AND 
                            ([PGRu2].[attribute_id] IS NULL OR [PGRu2].[attribute_id] = @AttributeId)
                    )
                    OR @HasGrantPermissionsAnyUserPermission = 1)   
                )

                AND

                -- [Block 2: account-level check (LAWYER vs CUSTOMER)]

                (
                    CASE 
                        WHEN @AttributeName = 'LAWYER' THEN
                            CASE 

                                -- [Layer 1: Ownership]

                                WHEN ([L].[user_id] = @ExternalUserId AND (@HasGrantPermissionsOwnLawyerAccountUserPermission OR @HasGrantPermissionsAnyLawyerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER]

                                WHEN (
                                    @GrantPermissionsLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                                        WHERE
                                            [PGRu2].[related_user_id] = @UserId                                        AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                AND 
                                            [PGRu2].[role_id]         = @RoleId                                        AND 
                                            [PGRu2].[permission_id]   = @GrantPermissionsLawyerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [PGRu2].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasGrantPermissionsAnyLawyerAccountUserPermission = 1
                                ) THEN 1 

                                ELSE 0
                            END
                        WHEN @AttributeName = 'CUSTOMER' THEN
                            CASE 

                                -- [Layer 1: Ownership]

                                WHEN ([C].[user_id] = @ExternalUserId AND (@HasGrantPermissionsOwnCustomerAccountUserPermission OR @HasGrantPermissionsAnyCustomerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER]

                                WHEN (
                                    @GrantPermissionsCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                                        WHERE
                                            [PGRu2].[related_user_id] = @UserId                                          AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                  AND 
                                            [PGRu2].[role_id]         = @RoleId                                          AND 
                                            [PGRu2].[permission_id]   = @GrantPermissionsCustomerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [PGRu2].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasGrantPermissionsAnyCustomerAccountUserPermission = 1
                                ) THEN 1 

                                ELSE 0
                            END
                        ELSE 0
                    END = 1
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customers] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
)

SELECT
    CASE WHEN [view_permission].[apply] = 1 AND [grant_permission].[apply] = 1 THEN 1 
    ELSE 0 END AS [result]
FROM [view_permission], [grant_permission];";

                var result = await connection.Connection.QueryFirstAsync<bool>(
                      new CommandDefinition(
                              commandText:       queryText,
                              parameters:        queryParameters,
                              transaction:       connection.Transaction,
                              cancellationToken: contextualizer.CancellationToken,
                              commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

                return result;

            });

            if (!hasPermissionToAssignUser)
            {
                resultContructor.SetConstructor(new GrantPermissionsToCaseForSpecificUserDeniedError()
                {
                    Status = 400
                });

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }
        }

        if (!internalValues.Data.Items.Any())
        {
            resultConstructor.SetConstructor(new GrantPermissionsToCaseSuccess()
            {
                Details = new()
                {
                    IncludedItems = 0,

                    Result = internalValues.Data.Finished.Values.Select((item) =>
                        new GrantPermissionsToCaseSuccess.DetailsVariation.Fields
                        {
                            UserId       = item.UserId,
                            PermissionId = item.PermissionId,
                            AttributeId  = item.AttributeId,
                            RoleId       = item.RoleId,

                            Result = item.Result
                        })
                }
            });

            return resultConstructor.Build();
        }

        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var items = internalValues.Data.Items.Values.Select(x => new
            {
                RelatedCaseId = parameters.RelatedCaseId,

                AttributeId  = x.AttributeId,
                PermissionId = x.PermissionId,
                UserId       = x.UserId,
                RoleId       = x.RoleId
            });

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@" INSERT OR IGNORE INTO [permission_grants_case] ([related_case_id], [permission_id], [role_id], [user_id], [attribute_id]) 
                                                                            VALUES (@RelatedCaseId, @PermissionId, @RoleId, @UserId, @AttributeId);");

            var includedItems = await connection.Connection.ExecuteAsync(stringBuilder.ToString(), items);

            return includedItems;
        });

        resultConstructor.SetConstructor(new GrantPermissionsToCaseSuccess()
        {
            Details = new()
            {
                IncludedItems = includedItems,

                Result = internalValues.Data.Finished.Values.Select((item) =>
                    new GrantPermissionsToCaseSuccess.DetailsVariation.Fields
                    {
                        UserId       = item.UserId,
                        PermissionId = item.PermissionId,
                        AttributeId  = item.AttributeId,
                        RoleId       = item.RoleId,

                        Result = item.Result
                    })
            }
        });

        return resultConstructor.Build();
    }

    #endregion

    #region RevokePermissionsToCaseAsync

    public async Task<Result> RevokePermissionsToCaseAsync(RevokePermissionsToCaseParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            RevokePermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CASE, contextualizer),

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            RevokePermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_USER, contextualizer),
            RevokePermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            // [CASE]

            RevokePermissionsOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_CASE, contextualizer),

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            // [USER]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            // [CASE]

            RevokePermissionsAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_CASE, contextualizer),

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer),

            // [USER]

            RevokePermissionsAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
            RevokePermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            RevokePermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_USER, contextualizer),
            RevokePermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)

        };

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(roleIdResult);

        // [Related Case Id]
        var relatedCaseIdResult = await ValidateCaseId(
            parameters.RelatedCaseId,
            contextualizer);

        if (relatedCaseIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(relatedCaseIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build().Incorporate(attributeAccountResult);

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasRevokePermissionsAnyCasePermission',                @RevokePermissionsAnyCasePermissionId),
    ('HasRevokePermissionsOwnCasePermission',                @RevokePermissionsOwnCasePermissionId),
    ('HasRevokePermissionsCasePermission',                   @RevokePermissionsCasePermissionId),
    ('HasViewAnyCasePermission',                             @ViewAnyCasePermissionId),
    ('HasViewOwnCasePermission',                             @ViewOwnCasePermissionId),
    ('HasViewPublicCasePermission',                          @ViewPublicCasePermissionId),
    ('HasViewCasePermission',                                @ViewCasePermissionId),

    ('HasRevokePermissionsAnyUserPermission',                @RevokePermissionsAnyUserPermissionId),
    ('HasRevokePermissionsAnyLawyerAccountUserPermission',   @RevokePermissionsAnyLawyerAccountUserPermissionId),
    ('HasRevokePermissionsAnyCustomerAccountUserPermission', @RevokePermissionsAnyCustomerAccountUserPermissionId),
    ('HasRevokePermissionsOwnUserPermission',                @RevokePermissionsOwnUserPermissionId),
    ('HasRevokePermissionsOwnLawyerAccountUserPermission',   @RevokePermissionsOwnLawyerAccountUserPermissionId),
    ('HasRevokePermissionsOwnCustomerAccountUserPermission', @RevokePermissionsOwnCustomerAccountUserPermissionId),
    ('HasRevokePermissionsUserPermission',                   @RevokePermissionsUserPermissionId),
    ('HasRevokePermissionsLawyerAccountUserPermission',      @RevokePermissionsLawyerAccountUserPermissionId),
    ('HasRevokePermissionsCustomerAccountUserPermission',    @RevokePermissionsCustomerAccountUserPermissionId),
    ('HasViewOwnUserPermission',                             @ViewOwnUserPermissionId),
    ('HasViewOwnLawyerAccountUserPermission',                @ViewOwnLawyerAccountUserPermissionId),
    ('HasViewOwnCustomerAccountUserPermission',              @ViewOwnCustomerAccountUserPermissionId),
    ('HasViewPublicUserPermission',                          @ViewPublicUserPermissionId),
    ('HasViewPublicLawyerAccountUserPermission',             @ViewPublicLawyerAccountUserPermissionId),
    ('HasViewPublicCustomerAccountUserPermission',           @ViewPublicCustomerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                             @ViewAnyUserPermissionId),
    ('HasViewAnyLawyerAccountUserPermission',                @ViewAnyLawyerAccountUserPermissionId),
    ('HasViewAnyCustomerAccountUserPermission',              @ViewAnyCustomerAccountUserPermissionId),
    ('HasViewUserPermission',                                @ViewUserPermissionId),
    ('HasViewLawyerAccountUserPermission',                   @ViewLawyerAccountUserPermissionId),
    ('HasViewCustomerAccountUserPermission',                 @ViewCustomerAccountUserPermissionId)
),
[grants] AS (

    -- [user grants]
    SELECT 
        [PC].[permission_name], 
        [PGU].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId

    UNION

    -- [role grants]
    SELECT 
        [PC].[permission_name], 
        [PG].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND
         [PG].[role_id]       = @RoleId

    UNION

    -- [case ACL grants]
    SELECT 
        [PC].[permission_name], 
        [PGC].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_case] [PGC]
      ON [PGC].[permission_id]   = [PC].[permission_id] AND 
         [PGC].[user_id]         = @UserId              AND 
         [PGC].[role_id]         = @RoleId              AND 
         [PGC].[related_case_id] = @RelatedCaseId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission'           AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'                 AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCasePermission],

    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyUserPermission'                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyLawyerAccountUserPermission'   AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyCustomerAccountUserPermission' AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnUserPermission'                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnLawyerAccountUserPermission'   AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnCustomerAccountUserPermission' AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsUserPermission'                   AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsLawyerAccountUserPermission'      AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsCustomerAccountUserPermission'    AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                             AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'              AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                          AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission'             AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission'           AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                             AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'              AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUserPermission'                                AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewLawyerAccountUserPermission'                   AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCustomerAccountUserPermission'                 AND [G].[attribute_id] IS NULL THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCustomerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {

            RevokePermissionsAnyCasePermissionId = permission.RevokePermissionsAnyCasePermissionId,
            RevokePermissionsOwnCasePermissionId = permission.RevokePermissionsOwnCasePermissionId,
            RevokePermissionsCasePermissionId    = permission.RevokePermissionsCasePermissionId,

            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,
            ViewCasePermissionId       = permission.ViewCasePermissionId,

            RevokePermissionsAnyUserPermissionId                = permission.RevokePermissionsAnyUserPermissionId,
            RevokePermissionsAnyLawyerAccountUserPermissionId   = permission.RevokePermissionsAnyLawyerAccountUserPermissionId,
            RevokePermissionsAnyCustomerAccountUserPermissionId = permission.RevokePermissionsAnyCustomerAccountUserPermissionId,

            RevokePermissionsOwnUserPermissionId                = permission.RevokePermissionsOwnUserPermissionId,
            RevokePermissionsOwnLawyerAccountUserPermissionId   = permission.RevokePermissionsOwnLawyerAccountUserPermissionId,
            RevokePermissionsOwnCustomerAccountUserPermissionId = permission.RevokePermissionsOwnCustomerAccountUserPermissionId,

            RevokePermissionsUserPermissionId                = permission.RevokePermissionsUserPermissionId,
            RevokePermissionsLawyerAccountUserPermissionId   = permission.RevokePermissionsLawyerAccountUserPermissionId,
            RevokePermissionsCustomerAccountUserPermissionId = permission.RevokePermissionsCustomerAccountUserPermissionId,

            ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,
            ViewOwnLawyerAccountUserPermissionId   = permission.ViewOwnLawyerAccountUserPermissionId,
            ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

            ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,
            ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,
            ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

            ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
            ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
            ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

            ViewUserPermissionId                = permission.ViewUserPermissionId,
            ViewLawyerAccountUserPermissionId   = permission.ViewLawyerAccountUserPermissionId,
            ViewCustomerAccountUserPermissionId = permission.ViewCustomerAccountUserPermissionId,

            UserId        = parameters.UserId,
            RelatedCaseId = parameters.RelatedCaseId,
            AttributeId   = parameters.AttributeId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.RevokePermissionsToCase>(queryPermissions, queryPermissionsParameters);

        // [Case Information]

        const string queryCaseInformations = "SELECT [C].[Private], CASE WHEN [C].[user_id] = @UserId THEN 1 ELSE 0 END AS [Owner] FROM [cases] [C] WHERE [C].[id] = @RelatedCaseId";

        var queryCaseInformationParameters = new
        {
            UserId = parameters.UserId,
            RelatedCaseId = parameters.RelatedCaseId
        };

        var caseInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner)>(queryCaseInformations, queryCaseInformationParameters);

        // [VIEW]
        if (((caseInformationResult.Private.HasValue && caseInformationResult.Private.Value) && !permissionsResult.HasViewPublicCasePermission) &&
            ((caseInformationResult.Owner.HasValue   && caseInformationResult.Owner.Value)   && !permissionsResult.HasViewOwnCasePermission)    &&
            !permissionsResult.HasViewCasePermission &&
            !permissionsResult.HasViewAnyCasePermission)
        {
            resultConstructor.SetConstructor(new CaseNotFoundError());

            return resultConstructor.Build();
        }

        // [REVOKE_PERMISSIONS]
        if (((caseInformationResult.Owner.HasValue && caseInformationResult.Owner.Value) && !permissionsResult.HasRevokePermissionsOwnCasePermission) &&
            !permissionsResult.HasRevokePermissionsCasePermission &&
            !permissionsResult.HasRevokePermissionsAnyCasePermission)
        {
            resultConstructor.SetConstructor(new RevokePermissionsToCaseDeniedError());

            return resultConstructor.Build();
        }

        // [Permission Objects Validations]

        var distinctPermission = parameters.Permissions.Distinct();

        var internalValues = new InternalValues.RevokePermissionsToCase()
        {
            Data = new()
            {
                Items = distinctPermission
                    .Distinct()
                    .ToDictionary(
                        x => x.Id,
                        x => new InternalValues.RevokePermissionsToCase.DataPropreties.Item()
                        {
                            Id = x.Id,

                            // [From parameters]

                            UserId       = x.UserId,
                            PermissionId = x.PermissionId,
                            RoleId       = x.RoleId,
                            AttributeId  = x.AttributeId
                        })
            }
        };

        // [User Id]
        var userIdResultDictionary = await ValidateUserId(
            internalValues.Data.Items.Values
                .Select(x => x.UserId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!userIdResultDictionary.TryGetValue(item.UserId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Attribute Id]
        var attributeIdResultDictionary = await ValidateAttributeId(
            internalValues.Data.Items.Values
                .Select(x => x.AttributeId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!attributeIdResultDictionary.TryGetValue(item.AttributeId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Role Id]
        var roleIdResultDictionary = await ValidateRoleId(
            internalValues.Data.Items.Values
                .Select(x => x.RoleId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!roleIdResultDictionary.TryGetValue(item.RoleId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Permission Id]
        var permissionIdResultDictionary = await ValidatePermissionId(
            internalValues.Data.Items.Values
                .Select(x => x.PermissionId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!permissionIdResultDictionary.TryGetValue(item.PermissionId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Attribute Account]
        var attributeAccountResultDictionary = await ValidateAttributeAccount(
            internalValues.Data.Items.Values
                .Select(x => (UserId: x.UserId, AttributeId: x.AttributeId))
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!attributeAccountResultDictionary.TryGetValue((UserId: item.UserId, AttributeId: item.AttributeId), out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        const string queryAttributes = "SELECT [A].[id] AS [Id], [A].[name] AS [Name] FROM [Attributes] [A]";

        var attributes = await connection.Connection.QueryAsync<(int Id, string Name)>(queryAttributes);

        const string queryAllowedPermissions = @"
SELECT [P].[id] AS [Id] FROM [Permissions] [P] WHERE [P].[name] IN 
('VIEW_CASE', 'VIEW_PERMISSIONS_CASE', 'ASSIGN_LAWYER_CASE', 'ASSIGN_CUSTOMER_CASE', 'ASSIGN_CUSTOMER_CASE', 'GRANT_PERMISSIONS_CASE', 'REVOKE_PERMISSIONS_CASE')";

        var allowedPermissions = await connection.Connection.QueryAsync<int>(queryAllowedPermissions);

        foreach (var item in internalValues.Data.Items.Values)
        {
            var resultContructor = new ResultConstructor();

            if (!allowedPermissions.Contains(item.PermissionId))
            {
                resultContructor.SetConstructor(new ForbiddenPermissionsToRevokeToCaseError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }

            var hasPermissionToAssignUser = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    HasViewOwnUserPermission = permissionsResult.HasViewOwnUserPermission,
                    HasViewAnyUserPermission = permissionsResult.HasViewAnyUserPermission,

                    HasViewPublicUserPermission             = permissionsResult.HasViewPublicUserPermission,
                    HasViewOwnLawyerAccountUserPermission   = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                    HasViewAnyLawyerAccountUserPermission   = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                   
                    HasViewPublicLawyerAccountUserPermission   = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                    HasViewOwnCustomerAccountUserPermission    = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                    HasViewAnyCustomerAccountUserPermission    = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                    HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,

                    HasRevokePermissionsOwnUserPermission = permissionsResult.HasRevokePermissionsOwnUserPermission,
                    HasRevokePermissionsAnyUserPermission = permissionsResult.HasRevokePermissionsAnyUserPermission,

                    HasRevokePermissionsOwnLawyerAccountUserPermission = permissionsResult.HasRevokePermissionsOwnLawyerAccountUserPermission,
                    HasRevokePermissionsAnyLawyerAccountUserPermission = permissionsResult.HasRevokePermissionsAnyLawyerAccountUserPermission,

                    HasRevokePermissionsOwnCustomerAccountUserPermission = permissionsResult.HasRevokePermissionsOwnCustomerAccountUserPermission,
                    HasRevokePermissionsAnyCustomerAccountUserPermission = permissionsResult.HasRevokePermissionsAnyCustomerAccountUserPermission,
 
                    ViewUserPermissionId                = permission.ViewUserPermissionId,
                    ViewLawyerAccountUserPermissionId   = permission.ViewLawyerAccountUserPermissionId,
                    ViewCustomerAccountUserPermissionId = permission.ViewCustomerAccountUserPermissionId,

                    RevokePermissionsUserPermissionId                 = permission.RevokePermissionsUserPermissionId,
                    RevokePermissionsLawyerAccountUserPermissionId    = permission.RevokePermissionsLawyerAccountUserPermissionId,
                    RevokePermissionsCustomerAccountUserPermissionId  = permission.RevokePermissionsCustomerAccountUserPermissionId,

                    AttributeName = attributes.First(x => x.Id == item.AttributeId).Name,

                    AttributeId  = item.AttributeId,
                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId,

                    RelatedCaseId  = parameters.RelatedCaseId,

                    ExternalUserId = parameters.UserId,
                };

                var queryText = @$"
WITH [view_permission] AS (
    SELECT
        CASE
            WHEN

                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasViewOwnUserPermission OR @HasViewAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_USER]

                    ([U].[private] = 1 AND (
                        @ViewUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1 FROM [permission_grants_relationship] [PGRu]
                            WHERE
                                [PGRu].[related_user_id] = @UserId               AND 
                                [PGRu].[user_id]         = @ExternalUserId       AND 
                                [PGRu].[role_id]         = @RoleId               AND 
                                [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                                ([PGRu].[attribute_id] IS NULL OR [PGRu].[attribute_id] = @AttributeId)
                        )
                        OR @HasViewAnyUserPermission = 1
                    ))

                    OR 

                    -- [Layer 3: Public]

                    ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                )

                AND

                -- [Block 2: account-level check (LAWYER vs CUSTOMER)]

                (
                    CASE 
                        WHEN @AttributeName = 'LAWYER' THEN
                            CASE
                                -- [Layer 1: Ownership]

                                WHEN ([L].[user_id] = @ExternalUserId AND (@HasViewOwnLawyerAccountUserPermission OR @HasViewAnyLawyerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_LAWYER_ACCOUNT_USER]

                                WHEN [L].[private] = 1 AND (
                                    @ViewLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRl]
                                        WHERE
                                            [PGRl].[related_user_id] = @UserId                            AND
                                            [PGRl].[user_id]         = @ExternalUserId                    AND
                                            [PGRl].[role_id]         = @RoleId                            AND
                                            [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
                                            ([PGRl].[attribute_id] IS NULL OR [PGRl].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasViewAnyLawyerAccountUserPermission = 1
                                ) THEN 1

                                -- [Layer 3: Public]

                                WHEN ([L].[private] = 0 AND (@HasViewPublicLawyerAccountUserPermission = 1 OR @HasViewAnyLawyerAccountUserPermission = 1)) THEN 1

                                ELSE 0
                            END
                        WHEN @AttributeName = 'CUSTOMER' THEN
                            CASE
                                -- [Layer 1: Ownership]

                                WHEN ([C].[user_id] = @ExternalUserId AND (@HasViewOwnCustomerAccountUserPermission OR @HasViewAnyCustomerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_CUSTOMER_ACCOUNT_USER]

                                WHEN [C].[private] = 1 AND (
                                    @ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRc]
                                        WHERE
                                            [PGRc].[related_user_id] = @UserId                             AND 
                                            [PGRc].[user_id]         = @ExternalUserId                      AND 
                                            [PGRc].[role_id]         = @RoleId                              AND 
                                            [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                            ([PGRc].[attribute_id] IS NULL OR [PGRc].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasViewAnyCustomerAccountUserPermission = 1
                                ) THEN 1

                                -- [Layer 3: Public]

                                WHEN ([C].[private] = 0 AND (@HasViewPublicCustomerAccountUserPermission = 1 OR @HasViewAnyCustomerAccountUserPermission = 1)) THEN 1
                                
                                ELSE 0
                            END
                        ELSE 0
                    END = 1
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customers] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
),
[revoke_permission] AS (
    SELECT
        CASE
            WHEN
                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasRevokePermissionsOwnUserPermission OR @HasRevokePermissionsAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [REVOKE_PERMISSIONS_USER]

                    (@RevokePermissionsUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                        WHERE
                            [PGRu2].[related_user_id] = @UserId                            AND 
                            [PGRu2].[user_id]         = @ExternalUserId                    AND 
                            [PGRu2].[role_id]         = @RoleId                            AND 
                            [PGRu2].[permission_id]   = @RevokePermissionsUserPermissionId AND 
                            ([PGRu2].[attribute_id] IS NULL OR [PGRu2].[attribute_id] = @AttributeId)
                    )
                    OR @HasRevokePermissionsAnyUserPermission = 1)   
                )

                AND

                -- [Block 2: account-level check (LAWYER vs CUSTOMER)]

                (
                    CASE 
                        WHEN @AttributeName = 'LAWYER' THEN
                            CASE 

                                -- [Layer 1: Ownership]

                                WHEN ([L].[user_id] = @ExternalUserId AND (@HasRevokePermissionsOwnLawyerAccountUserPermission OR @HasRevokePermissionsAnyLawyerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER]

                                WHEN (
                                    @RevokePermissionsLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                                        WHERE 
                                            [PGRu2].[related_user_id] = @UserId                                        AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                 AND 
                                            [PGRu2].[role_id]         = @RoleId                                         AND 
                                            [PGRu2].[permission_id]   = @RevokePermissionsLawyerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [PGRu2].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasRevokePermissionsAnyLawyerAccountUserPermission = 1
                                ) THEN 1 

                                ELSE 0
                            END
                        WHEN @AttributeName = 'CUSTOMER' THEN
                            CASE 

                                -- [Layer 1: Ownership]

                                WHEN ([C].[user_id] = @ExternalUserId AND (@HasRevokePermissionsOwnCustomerAccountUserPermission OR @HasRevokePermissionsAnyCustomerAccountUserPermission = 1)) THEN 1

                                -- [Layer 2: permission_grants_relationship (ACL Grant)] [REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER]

                                WHEN (
                                    @RevokePermissionsCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                                        WHERE
                                            [PGRu2].[related_user_id] = @UserId                                           AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                   AND 
                                            [PGRu2].[role_id]         = @RoleId                                           AND 
                                            [PGRu2].[permission_id]   = @RevokePermissionsCustomerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [PGRu2].[attribute_id] = @AttributeId)
                                    )
                                    OR @HasRevokePermissionsAnyCustomerAccountUserPermission = 1
                                ) THEN 1 

                                ELSE 0
                            END
                        ELSE 0
                    END = 1
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customers] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
)

SELECT
    CASE WHEN [view_permission].[apply] = 1 AND [revoke_permission].[apply] = 1 THEN 1 
    ELSE 0 END AS [result]
FROM [view_permission], [revoke_permission];";

                var result = await connection.Connection.QueryFirstAsync<bool>(
                  new CommandDefinition(
                          commandText:       queryText,
                          parameters:        queryParameters,
                          transaction:       connection.Transaction,
                          cancellationToken: contextualizer.CancellationToken,
                          commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

                return result;

            });

            if (!hasPermissionToAssignUser)
            {
                resultContructor.SetConstructor(new RevokePermissionsToCaseForSpecificUserDeniedError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }
        }

        if (!internalValues.Data.Items.Any())
        {
            resultConstructor.SetConstructor(new RevokePermissionsToCaseSuccess()
            {
                Details = new()
                {
                    DeletedItems = 0,

                    Result = internalValues.Data.Finished.Values.Select((item) =>
                        new RevokePermissionsToCaseSuccess.DetailsVariation.Fields
                        {
                            UserId       = item.UserId,
                            PermissionId = item.PermissionId,
                            AttributeId  = item.AttributeId,
                            RoleId       = item.RoleId,

                            Result = item.Result
                        })
                }
            });

            return resultConstructor.Build();
        }

        var deletedItems = await ValuesExtensions.GetValue(async () =>
        {
            var dynamicParameter = new DynamicParameters();

            dynamicParameter.Add($"RelatedCaseId", parameters.RelatedCaseId);

            var stringBuilder = new StringBuilder();

            foreach (var (item, index) in internalValues.Data.Items.Values.Select((item, index) => (item, index)))
            {
                dynamicParameter.Add($"PermissionId({index})", item.PermissionId);
                dynamicParameter.Add($"UserId({index})", item.UserId);
                dynamicParameter.Add($"RoleId({index})", item.RoleId);
                dynamicParameter.Add($"AttributeId({index})", item.AttributeId);

                stringBuilder.Append($"DELETE FROM [permission_grants_case] WHERE [related_case_id] = @RelatedCaseId AND [user_id] = @UserId({index}) AND [permission_id] = @PermissionId({index}) AND [user_id] = @UserId({index}) AND [attribute_id] = @AttributeId({index});");
            }

            var deletedItems = await connection.Connection.ExecuteAsync(stringBuilder.ToString(), dynamicParameter);

            return deletedItems;
        });

        resultConstructor.SetConstructor(new RevokePermissionsToCaseSuccess()
        {
            Details = new()
            {
                DeletedItems = deletedItems,

                Result = internalValues.Data.Finished.Values.Select((item) =>
                    new RevokePermissionsToCaseSuccess.DetailsVariation.Fields
                    {
                        UserId       = item.UserId,
                        PermissionId = item.PermissionId,
                        AttributeId  = item.AttributeId,
                        RoleId       = item.RoleId,

                        Result = item.Result
                    })
            }
        });

        return resultConstructor.Build();
    }

    #endregion

    #endregion

    #region User

    #region EnlistPermissionsFromUserAsync

    public async Task<Result<EnlistedPermissionsFromUserInformation>> EnlistPermissionsFromUserAsync(EnlistPermissionsFromUserParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<EnlistedPermissionsFromUserInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromUserInformation>().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<EnlistedPermissionsFromUserInformation>().Incorporate(roleIdResult);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_USER, contextualizer),
            ViewPermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_USER, contextualizer),
            ViewPermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPermissionsAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_USER, contextualizer),
            ViewPermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
        };

        var result = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewPermissionsAnyUserPermission', @ViewPermissionsAnyUserPermissionId),
    ('HasViewPermissionsOwnUserPermission', @ViewPermissionsOwnUserPermissionId),
    ('HasViewPermissionsUserPermission',    @ViewPermissionsUserPermissionId),
    ('HasViewOwnUserPermission',            @ViewOwnUserPermissionId),
    ('HasViewPublicUserPermission',         @ViewPublicUserPermissionId),
    ('HasViewAnyUserPermission',            @ViewAnyUserPermissionId),
    ('HasViewUserPermission',               @ViewUserPermissionId)
),
[grants] AS (

    -- [user grants]
    SELECT 
        [PC].[permission_name], 
        [PGU].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId

    UNION

    -- [role grants]
    SELECT
        [PC].[permission_name],
        [PG].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND
         [PG].[role_id]       = @RoleId

    UNION

    -- [case ACL grants]
    SELECT 
        [PC].[permission_name], 
        [PGR].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_relationship] [PGR]
      ON [PGR].[permission_id]   = [PC].[permission_id] AND 
         [PGR].[user_id]         = @UserId              AND 
         [PGR].[role_id]         = @RoleId              AND 
         [PGR].[related_user_id] = @RelatedUserId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsAnyUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPermissionsAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsOwnUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPermissionsOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPermissionsUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'            THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'         THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'            THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUserPermission'               THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new 
            { 
                ViewPermissionsAnyUserPermissionId  = permission.ViewPermissionsAnyUserPermissionId,               
                ViewPermissionsOwnUserPermissionId  = permission.ViewPermissionsOwnUserPermissionId,               
                ViewPermissionsUserPermissionId     = permission.ViewPermissionsUserPermissionId,               

                ViewOwnUserPermissionId    = permission.ViewOwnUserPermissionId,               
                ViewPublicUserPermissionId = permission.ViewPublicUserPermissionId,               
                ViewAnyUserPermissionId    = permission.ViewAnyUserPermissionId,
                ViewUserPermissionId       = permission.ViewUserPermissionId,               

                UserId        = parameters.UserId,
                RelatedUserId = parameters.RelatedUserId,
                RoleId        = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.EnlistPermissionsFromUser>(queryPermissions, queryPermissionsParameters);

            // [User Information]

            const string queryUserInformations = @"
SELECT 
    [U].[private]                                           AS [Private], 
    (SELECT CASE WHEN [U].[id] = @UserId THEN 1 ELSE 0 END) AS [Owner]
FROM [users] [U] WHERE [U].[id] = @RelatedUserId";

            var queryUserInformationParameters = new
            {
                RelatedUserId = parameters.RelatedUserId,
                UserId = parameters.UserId
            };

            var userInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner)>(queryUserInformations, queryUserInformationParameters);

            // [VIEW]
            if (((userInformationResult.Private.HasValue && userInformationResult.Private.Value) && !permissionsResult.HasViewPublicUserPermission) &&
                ((userInformationResult.Owner.HasValue   && userInformationResult.Owner.Value)   && !permissionsResult.HasViewOwnUserPermission)    &&
                !permissionsResult.HasViewUserPermission &&
                !permissionsResult.HasViewAnyUserPermission)
            {
                resultConstructor.SetConstructor(new UserNotFoundError());

                return resultConstructor.Build<EnlistedPermissionsFromUserInformation>();
            }

            // [VIEW_PERMISSIONS]
            if (((userInformationResult.Owner.HasValue && userInformationResult.Owner.Value) && !permissionsResult.HasViewPermissionsOwnUserPermission) &&
                !permissionsResult.HasViewPermissionsUserPermission &&
                !permissionsResult.HasViewPermissionsAnyUserPermission)
            {
                resultConstructor.SetConstructor(new EnlistPermissionsFromUserDeniedError());

                return resultConstructor.Build<EnlistedPermissionsFromUserInformation>();
            }

            // [Principal Query]

            var queryParameters = new
            {
                 UserId        = parameters.UserId,
                 RelatedUserId = parameters.RelatedUserId
            };

            var queryText = $@"
SELECT
   [U].[name] AS [UserName],
   [P].[name] AS [PermissionName],
   [R].[name] AS [RoleName],
   [U].[id]   AS [UserId],
   [P].[id]   AS [PermissionId],
   [R].[id]   AS [RoleId]
FROM [permission_grants_relationship] [PGR]
LEFT JOIN [users]       [U] ON [U].[id] = [PGR].[user_id]
LEFT JOIN [permissions] [P] ON [P].[id] = [PGR].[permission_id]
LEFT JOIN [roles]       [R] ON [R].[id] = [PGR].[role_id]
WHERE [U].[id] = @RelatedUserId";

            EnlistedPermissionsFromUserInformation information;

            using (var multiple = await connection.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds
                    )))
            {
                information = new EnlistedPermissionsFromUserInformation
                {
                    Items = await multiple.ReadAsync<EnlistedPermissionsFromUserInformation.ItemProperties>()
                };
            }

            return resultConstructor.Build<EnlistedPermissionsFromUserInformation>(information);
        });

        return result;
    }


    #endregion

    #region GlobalPermissionsRelatedWithUserAsync

    public async Task<Result<GlobalPermissionsRelatedWithUserInformation>> GlobalPermissionsRelatedWithUserAsync(GlobalPermissionsRelatedWithUserParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<GlobalPermissionsRelatedWithUserInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to USER or ROLE permission]

            RegisterUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REGISTER_USER, contextualizer),
            RegisterLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REGISTER_LAWYER_ACCOUNT_USER, contextualizer),
            RegisterCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REGISTER_CUSTOMER_ACCOUNT_USER, contextualizer),

            EditOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.EDIT_OWN_USER, contextualizer),
            EditOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.EDIT_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            EditOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_USER, contextualizer),
            ViewPermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            GrantPermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_USER, contextualizer),
            GrantPermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            RevokePermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_USER, contextualizer),
            RevokePermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_USER, contextualizer),
            RevokePermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            EditAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_USER, contextualizer),
            EditAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            EditAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPermissionsAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_USER, contextualizer),
            ViewPermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            GrantPermissionsAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_USER, contextualizer),
            GrantPermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            RevokePermissionsAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
            RevokePermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
            RevokePermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
        };

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<GlobalPermissionsRelatedWithUserInformation>().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<GlobalPermissionsRelatedWithUserInformation>().Incorporate(roleIdResult);

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
        ('HasRegisterUserPermission',                            @RegisterUserPermissionId),
        ('HasRegisterLawyerAccountUserPermission',               @RegisterLawyerAccountUserPermissionId),
        ('HasRegisterCustomerAccountUserPermission',             @RegisterCustomerAccountUserPermissionId),
        ('HasEditOwnUserPermission',                             @EditOwnUserPermissionId),
        ('HasEditAnyUserPermission',                             @EditAnyUserPermissionId),
        ('HasEditOwnLawyerAccountUserPermission',                @EditOwnLawyerAccountUserPermissionId),
        ('HasEditAnyLawyerAccountUserPermission',                @EditAnyLawyerAccountUserPermissionId),
        ('HasEditOwnCustomerAccountUserPermission',              @EditOwnCustomerAccountUserPermissionId),
        ('HasEditAnyCustomerAccountUserPermission',              @EditAnyCustomerAccountUserPermissionId),
        ('HasViewOwnUserPermission',                             @ViewOwnUserPermissionId),
        ('HasViewAnyUserPermission',                             @ViewAnyUserPermissionId),
        ('HasViewPublicUserPermission',                          @ViewPublicUserPermissionId),
        ('HasViewOwnLawyerAccountUserPermission',                @ViewOwnLawyerAccountUserPermissionId),
        ('HasViewAnyLawyerAccountUserPermission',                @ViewAnyLawyerAccountUserPermissionId),
        ('HasViewPublicLawyerAccountUserPermission',             @ViewPublicLawyerAccountUserPermissionId),
        ('HasViewOwnCustomerAccountUserPermission',              @ViewOwnCustomerAccountUserPermissionId),
        ('HasViewAnyCustomerAccountUserPermission',              @ViewAnyCustomerAccountUserPermissionId),
        ('HasViewPublicCustomerAccountUserPermission',           @ViewPublicCustomerAccountUserPermissionId),
        ('HasViewPermissionsOwnUserPermission',                  @ViewPermissionsOwnUserPermissionId),
        ('HasViewPermissionsAnyUserPermission',                  @ViewPermissionsAnyUserPermissionId),
        ('HasViewPermissionsOwnLawyerAccountUserPermission',     @ViewPermissionsOwnLawyerAccountUserPermissionId),
        ('HasViewPermissionsAnyLawyerAccountUserPermission',     @ViewPermissionsAnyLawyerAccountUserPermissionId),
        ('HasViewPermissionsOwnCustomerAccountUserPermission',   @ViewPermissionsOwnCustomerAccountUserPermissionId),
        ('HasViewPermissionsAnyCustomerAccountUserPermission',   @ViewPermissionsAnyCustomerAccountUserPermissionId),
        ('HasGrantPermissionsOwnUserPermission',                 @GrantPermissionsOwnUserPermissionId),
        ('HasGrantPermissionsAnyUserPermission',                 @GrantPermissionsAnyUserPermissionId),
        ('HasGrantPermissionsOwnLawyerAccountUserPermission',    @GrantPermissionsOwnLawyerAccountUserPermissionId),
        ('HasGrantPermissionsAnyLawyerAccountUserPermission',    @GrantPermissionsAnyLawyerAccountUserPermissionId),
        ('HasGrantPermissionsOwnCustomerAccountUserPermission',  @GrantPermissionsOwnCustomerAccountUserPermissionId),
        ('HasGrantPermissionsAnyCustomerAccountUserPermission',  @GrantPermissionsAnyCustomerAccountUserPermissionId),
        ('HasRevokePermissionsOwnUserPermission',                @RevokePermissionsOwnUserPermissionId),
        ('HasRevokePermissionsAnyUserPermission',                @RevokePermissionsAnyUserPermissionId),
        ('HasRevokePermissionsOwnLawyerAccountUserPermission',   @RevokePermissionsOwnLawyerAccountUserPermissionId),
        ('HasRevokePermissionsAnyLawyerAccountUserPermission',   @RevokePermissionsAnyLawyerAccountUserPermissionId),
        ('HasRevokePermissionsOwnCustomerAccountUserPermission', @RevokePermissionsOwnCustomerAccountUserPermissionId),
        ('HasRevokePermissionsAnyCustomerAccountUserPermission', @RevokePermissionsAnyCustomerAccountUserPermissionId)
),
[grants] AS (
    SELECT
        [PC].[permission_name],
        [PGU].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId
    UNION
    SELECT
        [PC].[permission_name],
        [PG].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND 
         [PG].[role_id]       = @RoleId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasRegisterUserPermission'                            THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RegisterUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRegisterLawyerAccountUserPermission'               THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RegisterLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRegisterCustomerAccountUserPermission'             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RegisterCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditOwnUserPermission'                             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditOwnUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditAnyUserPermission'                             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditAnyUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditOwnLawyerAccountUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditOwnLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditAnyLawyerAccountUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditAnyLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditOwnCustomerAccountUserPermission'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditOwnCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditAnyCustomerAccountUserPermission'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditAnyCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewOwnUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewAnyUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                          THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPublicUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewOwnLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewAnyLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission'             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPublicLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewOwnCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewAnyCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission'           THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPublicCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsOwnUserPermission'                  THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsOwnUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsAnyUserPermission'                  THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsAnyUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsOwnLawyerAccountUserPermission'     THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsOwnLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsAnyLawyerAccountUserPermission'     THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsAnyLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsOwnCustomerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsOwnCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsAnyCustomerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsAnyCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnUserPermission'                 THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsOwnUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyUserPermission'                 THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsAnyUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnLawyerAccountUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsOwnLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyLawyerAccountUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsAnyLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnCustomerAccountUserPermission'  THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsOwnCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyCustomerAccountUserPermission'  THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsAnyCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsOwnUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsAnyUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsOwnLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsAnyLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsOwnCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsAnyCustomerAccountUser]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            RegisterUserPermissionId                = permission.RegisterUserPermissionId,
            RegisterLawyerAccountUserPermissionId   = permission.RegisterLawyerAccountUserPermissionId,
            RegisterCustomerAccountUserPermissionId = permission.RegisterCustomerAccountUserPermissionId,

            EditOwnUserPermissionId                = permission.EditOwnUserPermissionId,
            EditOwnLawyerAccountUserPermissionId   = permission.EditOwnLawyerAccountUserPermissionId,
            EditOwnCustomerAccountUserPermissionId = permission.EditOwnCustomerAccountUserPermissionId,

            ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,
            ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,
            ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

            ViewOwnUserPermissionId                 = permission.ViewOwnUserPermissionId,
            ViewOwnLawyerAccountUserPermissionId    = permission.ViewOwnLawyerAccountUserPermissionId,
            ViewOwnCustomerAccountUserPermissionId  = permission.ViewOwnCustomerAccountUserPermissionId,

            ViewPermissionsOwnUserPermissionId                = permission.ViewPermissionsOwnUserPermissionId,
            ViewPermissionsOwnLawyerAccountUserPermissionId   = permission.ViewPermissionsOwnLawyerAccountUserPermissionId,
            ViewPermissionsOwnCustomerAccountUserPermissionId = permission.ViewPermissionsOwnCustomerAccountUserPermissionId,

            GrantPermissionsOwnUserPermissionId                = permission.GrantPermissionsOwnUserPermissionId,
            GrantPermissionsOwnLawyerAccountUserPermissionId   = permission.GrantPermissionsOwnLawyerAccountUserPermissionId,
            GrantPermissionsOwnCustomerAccountUserPermissionId = permission.GrantPermissionsOwnCustomerAccountUserPermissionId,

            RevokePermissionsOwnUserPermissionId                = permission.RevokePermissionsOwnUserPermissionId,
            RevokePermissionsOwnLawyerAccountUserPermissionId   = permission.RevokePermissionsOwnLawyerAccountUserPermissionId,
            RevokePermissionsOwnCustomerAccountUserPermissionId = permission.RevokePermissionsOwnCustomerAccountUserPermissionId,

            EditAnyUserPermissionId                = permission.EditAnyUserPermissionId,
            EditAnyLawyerAccountUserPermissionId   = permission.EditAnyLawyerAccountUserPermissionId,
            EditAnyCustomerAccountUserPermissionId = permission.EditAnyCustomerAccountUserPermissionId,

            ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
            ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
            ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

            ViewPermissionsAnyUserPermissionId                = permission.ViewPermissionsAnyUserPermissionId,
            ViewPermissionsAnyLawyerAccountUserPermissionId   = permission.ViewPermissionsAnyLawyerAccountUserPermissionId,
            ViewPermissionsAnyCustomerAccountUserPermissionId = permission.ViewPermissionsAnyCustomerAccountUserPermissionId,

            GrantPermissionsAnyUserPermissionId                = permission.GrantPermissionsAnyUserPermissionId,
            GrantPermissionsAnyLawyerAccountUserPermissionId   = permission.GrantPermissionsAnyLawyerAccountUserPermissionId,
            GrantPermissionsAnyCustomerAccountUserPermissionId = permission.GrantPermissionsAnyCustomerAccountUserPermissionId,

            RevokePermissionsAnyUserPermissionId                = permission.RevokePermissionsAnyUserPermissionId,
            RevokePermissionsAnyLawyerAccountUserPermissionId   = permission.RevokePermissionsAnyLawyerAccountUserPermissionId,
            RevokePermissionsAnyCustomerAccountUserPermissionId = permission.RevokePermissionsAnyCustomerAccountUserPermissionId,

            UserId = parameters.UserId,
            RoleId = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<GlobalPermissionsRelatedWithUserInformation>(queryPermissions, queryPermissionsParameters);

        return resultConstructor.Build<GlobalPermissionsRelatedWithUserInformation>(permissionsResult);
    }

    #endregion

    #region PermissionsRelatedWithUserAsync

    public async Task<Result<PermissionsRelatedWithUserInformation>> PermissionsRelatedWithUserAsync(PermissionsRelatedWithUserParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<PermissionsRelatedWithUserInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            EditUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.EDIT_USER, contextualizer),
            EditLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.EDIT_LAWYER_ACCOUNT_USER, contextualizer),
            EditCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_USER, contextualizer),
            ViewPermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            GrantPermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_USER, contextualizer),
            GrantPermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            RevokePermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_USER, contextualizer),
            RevokePermissionsLawyerAccounUserPermissionId    = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer)
        };

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<PermissionsRelatedWithUserInformation>().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<PermissionsRelatedWithUserInformation>().Incorporate(roleIdResult);

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
        ('HasGrantPermissionsUserPermission',        @GrantPermissionsUserPermissionId),
        ('HasGrantPermissionsLawyerAccountUser',     @GrantPermissionsLawyerAccountUserId),
        ('HasGrantPermissionsCustomerAccountUser',   @GrantPermissionsCustomerAccountUserId),
        ('HasRevokePermissionsUser',                 @RevokePermissionsUserPermissionId),
        ('HasRevokePermissionsLawyerAccountUser',    @RevokePermissionsLawyerAccountUserPermissionId),
        ('HasRevokePermissionsCustomerAccountUser',  @RevokePermissionsCustomerAccountUserPermissionId),
        ('HasEditUser,                               @EditUserPermissionId),
        ('HasEditLawyerAccountUser,                  @EditLawyerAccountUserPermissionId),
        ('HasEditCustomerAccountUser,                @EditCustomerAccountUserPermissionId),
        ('HasViewUser,                               @ViewUserPermissionId),
        ('HasViewLawyerAccountUser,                  @ViewLawyerAccountUserPermissionId),
        ('HasViewCustomerAccountUser,                @ViewCustomerAccountUserPermissionId),
        ('HasViewPermissionsUser,                    @ViewPermissionsUserPermissionId),
        ('HasViewPermissionsLawyerAccountUser,       @ViewPermissionsLawyerAccountUserPermissionId),
        ('HasViewPermissionsCustomerAccountUser,     @ViewPermissionsCustomerAccountUserPermissionId)
),
[grants] AS (
    SELECT
        [PC].[permission_name],
        [PGR].[attribute_id],
        1 AS [granted]
    FROM [user_permission_checks] [PC]
    JOIN [permission_grants_relationship] [PGR]
      ON [PGR].[permission_id]     = [PC].[permission_id] AND
         [PGR].[user_id]           = @UserId              AND
         [PGR].[role_id]           = @RoleId              AND
         [PGR].[related_user_id]   = @RelatedUserId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsUserPermission'       THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsLawyerAccountUser'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsCustomerAccountUser'  THEN COALESCE([G].[granted],0) ELSE 0 END) AS [GrantPermissionsCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsUser'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsLawyerAccountUser'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsCustomerAccountUser' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [RevokePermissionsCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditUser'                             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditLawyerAccountUser'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditCustomerAccountUser'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [EditCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUser'                             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewLawyerAccountUser'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCustomerAccountUser'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewCustomerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsUser'                  THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsLawyerAccountUser'     THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsLawyerAccountUser],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPermissionsCustomerAccountUser'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [ViewPermissionsCustomerAccountUser]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            EditUserPermissionId                = permission.EditUserPermissionId,
            EditLawyerAccountUserPermissionId   = permission.EditLawyerAccountUserPermissionId,
            EditCustomerAccountUserPermissionId = permission.EditCustomerAccountUserPermissionId,

            ViewUserPermissionId                = permission.ViewUserPermissionId,
            ViewLawyerAccountUserPermissionId   = permission.ViewLawyerAccountUserPermissionId,
            ViewCustomerAccountUserPermissionId = permission.ViewCustomerAccountUserPermissionId,

            ViewPermissionsUserPermissionId                = permission.ViewPermissionsUserPermissionId,
            ViewPermissionsLawyerAccountUserPermissionId   = permission.ViewPermissionsLawyerAccountUserPermissionId,
            ViewPermissionsCustomerAccountUserPermissionId = permission.ViewPermissionsCustomerAccountUserPermissionId,

            GrantPermissionsUserPermissionId                = permission.GrantPermissionsUserPermissionId,
            GrantPermissionsLawyerAccountUserPermissionId   = permission.GrantPermissionsLawyerAccountUserPermissionId,
            GrantPermissionsCustomerAccountUserPermissionId = permission.GrantPermissionsCustomerAccountUserPermissionId,

            RevokePermissionsUserPermissionId                = permission.RevokePermissionsUserPermissionId,
            RevokePermissionsLawyerAccounUserPermissionId    = permission.RevokePermissionsLawyerAccounUserPermissionId,
            RevokePermissionsCustomerAccountUserPermissionId = permission.RevokePermissionsCustomerAccountUserPermissionId,

            UserId        = parameters.UserId,
            RoleId        = parameters.RoleId,
            RelatedUserId = parameters.RelatedUserId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionsRelatedWithUserInformation>(queryPermissions, queryPermissionsParameters);

        return resultConstructor.Build<PermissionsRelatedWithUserInformation>(permissionsResult);
    }

    #endregion

    #region GrantPermissionsToUser

    public async Task<Result> GrantPermissionsToUserAsync(GrantPermissionsToUserParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            GrantPermissionsUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_USER, contextualizer),

            ViewUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),

            // [Related to USER or ROLE permission]

            GrantPermissionsOwnUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_USER, contextualizer),
          
            ViewPublicUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewOwnUserPermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            GrantPermissionsAnyUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_USER, contextualizer),

            ViewAnyUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
        };

        //var isUserOwnerOfTheRelatedUser = parameters.RelatedUserId == parameters.UserId;

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(roleIdResult);

        // [Related User Id]
        var relatedUserIdResult = await ValidateUserId(
            parameters.RelatedUserId,
            contextualizer);

        if (relatedUserIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(relatedUserIdResult);

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasGrantPermissionsAnyUserPermission', @GrantPermissionsAnyUserPermissionId),
    ('HasGrantPermissionsOwnUserPermission', @GrantPermissionsOwnUserPermissionId),
    ('HasGrantPermissionsUserPermission',    @GrantPermissionsUserPermissionId),
    ('HasViewOwnUserPermission',             @ViewOwnUserPermissionId),
    ('HasViewPublicUserPermission',          @ViewPublicUserPermissionId),
    ('HasViewAnyUserPermission',             @ViewAnyUserPermissionId),
    ('HasViewUserPermission',                @ViewUserPermissionId)
),
[grants] AS (

    -- [user grants]
    SELECT 
        [PC].[permission_name], 
        [PGU].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId

    UNION ALL

    -- [role grants]
    SELECT 
        [PC].[permission_name], 
        [PG].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND
         [PG].[role_id]       = @RoleId

    UNION ALL

    -- [case ACL grants]
    SELECT 
        [PC].[permission_name], 
        [PGR].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_relationship] [PGR]
      ON [PGR].[permission_id]   = [PC].[permission_id] AND 
         [PGR].[user_id]         = @UserId              AND 
         [PGR].[role_id]         = @RoleId              AND 
         [PGR].[related_user_id] = @RelatedUserId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'          THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'             THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            GrantPermissionsUserPermissionId    = permission.GrantPermissionsUserPermissionId,
            GrantPermissionsOwnUserPermissionId = permission.GrantPermissionsOwnUserPermissionId,
            GrantPermissionsAnyUserPermissionId = permission.GrantPermissionsAnyUserPermissionId,

            ViewOwnUserPermissionId    = permission.ViewOwnUserPermissionId,
            ViewAnyUserPermissionId    = permission.ViewAnyUserPermissionId,
            ViewPublicUserPermissionId = permission.ViewPublicUserPermissionId,
            ViewUserPermissionId       = permission.ViewUserPermissionId,

            UserId        = parameters.UserId,
            RelatedUserId = parameters.RelatedUserId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.GrantPermissionsToUser>(queryPermissions, queryPermissionsParameters);

        // [User Information]

        const string queryUserInformations = @"
SELECT 
    [U].[private]                                    AS [Private], 
    (CASE WHEN [U].[id] = @UserId THEN 1 ELSE 0 END) AS [Owner],
FROM [users] [U] WHERE [U].[id] = @RelatedUserId";

        var queryUserInformationParameters = new
        {
            RelatedUserId = parameters.RelatedUserId,
            UserId        = parameters.UserId
        };

        var userInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner)>(queryUserInformations, queryUserInformationParameters);

        // [VIEW]
        if (((userInformationResult.Private.HasValue && userInformationResult.Private.Value) && !permissionsResult.HasViewPublicUserPermission) &&
            ((userInformationResult.Owner.HasValue   && userInformationResult.Owner.Value)   && !permissionsResult.HasViewOwnUserPermission)    &&
            !permissionsResult.HasViewUserPermission &&
            !permissionsResult.HasViewAnyUserPermission)
        {
            resultConstructor.SetConstructor(new UserNotFoundError());

            return resultConstructor.Build();
        }

        // [GRANT_PERMISSIONS]
        if (((userInformationResult.Owner.HasValue && userInformationResult.Owner.Value) && !permissionsResult.HasGrantPermissionsOwnUserPermission) &&
            !permissionsResult.HasGrantPermissionsUserPermission &&
            !permissionsResult.HasGrantPermissionsAnyUserPermission)
        {
            resultConstructor.SetConstructor(new GrantPermissionsToUserDeniedError());

            return resultConstructor.Build();
        }

        var distinctPermission = parameters.Permissions.Distinct();

        var internalValues = new InternalValues.GrantPermissionsToUser()
        {
            Data = new()
            {
                Items = distinctPermission
                    .Distinct()
                    .ToDictionary(
                        x => x.Id,
                        x => new InternalValues.GrantPermissionsToUser.DataPropreties.Item()
                        {
                            Id = x.Id,

                            // [From parameters]

                            UserId       = x.UserId,
                            PermissionId = x.PermissionId,
                            RoleId       = x.RoleId,
                            AttributeId  = x.AttributeId
                        })
            }
        };

        // [User Id]
        var userIdResultDictionary = await ValidateUserId(
            internalValues.Data.Items.Values
                .Select(x => x.UserId)
                .Distinct(), 
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!userIdResultDictionary.TryGetValue(item.UserId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Role Id]
        var roleIdResultDictionary = await ValidateRoleId(
            internalValues.Data.Items.Values
                .Select(x => x.RoleId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!roleIdResultDictionary.TryGetValue(item.RoleId, out var result))
                continue;

            if(result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Attribute Id]
        var attributeIdResultDictionary = await ValidateAttributeId(
            internalValues.Data.Items.Values.Where(x => x.AttributeId.HasValue)
                .Select(x => x.AttributeId!.Value)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values.Where(x => x.AttributeId.HasValue))
        {
            if (!attributeIdResultDictionary.TryGetValue(item.AttributeId!.Value, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Permission Id]
        var permissionIdResultDictionary = await ValidatePermissionId(
            internalValues.Data.Items.Values
                .Select(x => x.PermissionId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!permissionIdResultDictionary.TryGetValue(item.PermissionId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        const string queryAllowedPermissions = @"
SELECT [P].[id] AS [Id] FROM [Permissions] WHERE [P].[name] IN 
('GRANT_PERMISSIONS_USER', 'REVOKE_PERMISSIONS_USER', 'GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER', 'REVOKE_PERMISSIONS_USER', 'CHAT_USER', 'VIEW_USER', 'VIEW_LAWYER_ACCOUNT_USER', 'VIEW_CUSTOMER_ACCOUNT_USER', 'VIEW_PERMISSIONS_USER', 'VIEW_PERMISSIONS_LAWYER_ACCOUNT_USER', 'VIEW_PERMISSIONS_CUSTOMER_ACCOUNT_USER', 'EDIT_USER');";

        var allowedPermissions = await connection.Connection.QueryAsync<int>(queryAllowedPermissions);

        foreach (var item in internalValues.Data.Items.Values)
        {
            var resultContructor = new ResultConstructor();

            if (!allowedPermissions.Contains(item.PermissionId))
            {
                resultContructor.SetConstructor(new ForbiddenPermissionsToGrantToUserError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }

            var hasPermissionToAssignUser = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    // [NOT ACL]

                    HasGrantPermissionsAnyUserPermission = permissionsResult.HasGrantPermissionsAnyUserPermission,
                    HasGrantPermissionsOwnUserPermission = permissionsResult.HasGrantPermissionsOwnUserPermission,
                  
                    HasViewAnyUserPermission    = permissionsResult.HasViewAnyUserPermission,
                    HasViewPublicUserPermission = permissionsResult.HasViewPublicUserPermission,
                    
                    // [ACL]

                    ViewUserPermissionId                = permission.ViewUserPermissionId,
                    ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                    ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,

                    GrantPermissionsUserPermissionId = permission.GrantPermissionsUserPermissionId,

                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId
                };

                const string queryText = @$"
WITH [view_permission] AS (
    SELECT
        CASE
            WHEN

                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasViewOwnUserPermission OR @HasViewAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_USER]

                    ([U].[private] = 1 AND (
                        @ViewUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1 FROM [permission_grants_relationship] [PGRu]
                            WHERE
                                [PGRu].[related_user_id] = @UserId               AND 
                                [PGRu].[user_id]         = @ExternalUserId       AND 
                                [PGRu].[role_id]         = @RoleId               AND 
                                [PGRu].[permission_id]   = @ViewUserPermissionId
                        )
                        OR @HasViewAnyUserPermission = 1
                    ))

                    OR 

                    -- [Layer 3: Public]

                    ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customer] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
),
[grant_permission] AS (
    SELECT
        CASE
            WHEN
                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasGrantPermissionsOwnUserPermission OR @HasGrantPermissionsAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSIONS_USER]

                    (@GrantPermissionsUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                        WHERE
                            [PGRu2].[related_user_id] = @UserId                           AND 
                            [PGRu2].[user_id]         = @ExternalUserId                   AND 
                            [PGRu2].[role_id]         = @RoleId                           AND 
                            [PGRu2].[permission_id]   = @GrantPermissionsUserPermissionId
                        )
                    )
                    OR @HasGrantPermissionsAnyUserPermission = 1)   
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customer] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
)
SELECT
    CASE WHEN [view_permission].[apply] = 1 AND [grant_permission].[apply] = 1 THEN 1 
    ELSE 0 END AS [result]
FROM [view_permission], [grant_permission];";

                    var result = await connection.Connection.QueryFirstAsync<bool>(
                      new CommandDefinition(
                              commandText:       queryText,
                              parameters:        queryParameters,
                              transaction:       connection.Transaction,
                              cancellationToken: contextualizer.CancellationToken,
                              commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

                    return result;

            });

            if (!hasPermissionToAssignUser)
            {
                resultContructor.SetConstructor(new GrantPermissionsToUserForSpecificUserDeniedError()
                {
                    Status = 400
                });

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }
        }

        if (!internalValues.Data.Items.Any())
        {
            resultConstructor.SetConstructor(new GrantPermissionsToUserSuccess()
            {
                Details = new()
                {
                    IncludedItems = 0,

                    Result = internalValues.Data.Finished.Values.Select((item) =>
                        new GrantPermissionsToUserSuccess.DetailsVariation.Fields
                        {
                            UserId       = item.UserId,
                            PermissionId = item.PermissionId,
                            RoleId       = item.RoleId,
                            AttributeId  = item.AttributeId,

                            Result = item.Result
                        })
                }
            });

            return resultConstructor.Build();
        }
          
        var includedItems = await ValuesExtensions.GetValue(async () =>
        { 
            var items = internalValues.Data.Items.Values.Select(x => new
            {
                RelatedUserId = parameters.RelatedUserId,

                PermissionId = x.PermissionId,
                UserId       = x.UserId,
                RoleId       = x.RoleId
            });

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@" INSERT OR IGNORE INTO [permission_grants_relationship] ([related_user_id], [permission_id], [role_id], [user_id], [attribute_id]) 
                                                                            VALUES (@RelatedUserId, @PermissionId, @RoleId, @UserId, @AttributeId);");

            var includedItems = await connection.Connection.ExecuteAsync(stringBuilder.ToString(), items);

            return includedItems;
        });

        resultConstructor.SetConstructor(new GrantPermissionsToUserSuccess()
        {
            Details = new()
            {
                IncludedItems = includedItems,

                Result = internalValues.Data.Finished.Values.Select((item) =>
                    new GrantPermissionsToUserSuccess.DetailsVariation.Fields
                    {
                        UserId       = item.UserId,
                        PermissionId = item.PermissionId,
                        RoleId       = item.RoleId,
                        AttributeId  = item.AttributeId,

                        Result = item.Result
                    })
            }
        });

        return resultConstructor.Build();
    }

    #endregion

    #region RevokePermissionsToUser

    public async Task<Result> RevokePermissionsToUserAsync(RevokePermissionsToUserParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            RevokePermissionsUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_USER, contextualizer),

            ViewUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),

            // [Related to USER or ROLE permission]

            RevokePermissionsOwnUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_USER, contextualizer),

            ViewPublicUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewOwnUserPermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            RevokePermissionsAnyUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
            
            ViewAnyUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer)
        };

        //var isUserOwnerOfTheRelatedUser = parameters.RelatedUserId == parameters.UserId;

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(roleIdResult);

        // [Related User Id]
        var relatedUserIdResult = await ValidateUserId(
            parameters.RelatedUserId,
            contextualizer);

        if (relatedUserIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(relatedUserIdResult);

      // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasRevokePermissionsAnyUserPermission', @RevokePermissionsAnyUserPermissionId),
    ('HasRevokePermissionsOwnUserPermission', @RevokePermissionsOwnUserPermissionId),
    ('HasRevokePermissionsUserPermission',    @RevokePermissionsUserPermissionId),
    ('HasViewOwnUserPermission',              @ViewOwnUserPermissionId),
    ('HasViewPublicUserPermission',           @ViewPublicUserPermissionId),
    ('HasViewAnyUserPermission',              @ViewAnyUserPermissionId),
    ('HasViewUserPermission',                 @ViewUserPermissionId)
),
[grants] AS (

    -- [user grants]
    SELECT 
        [PC].[permission_name], 
        [PGU].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId

    UNION

    -- [role grants]
    SELECT 
        [PC].[permission_name], 
        [PG].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND
         [PG].[role_id]       = @RoleId

    UNION

    -- [case ACL grants]
    SELECT 
        [PC].[permission_name], 
        [PGR].[attribute_id], 
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_relationship] [PGR]
      ON [PGR].[permission_id]   = [PC].[permission_id] AND 
         [PGR].[user_id]         = @UserId              AND 
         [PGR].[role_id]         = @RoleId              AND 
         [PGR].[related_user_id] = @RelatedUserId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'           THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'              THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUserPermission'                 THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            RevokePermissionsUserPermissionId    = permission.RevokePermissionsUserPermissionId,
            RevokePermissionOwnUserPermissionId  = permission.RevokePermissionsOwnUserPermissionId,
            RevokePermissionsAnyUserPermissionId = permission.RevokePermissionsAnyUserPermissionId,

            ViewOwnUserPermissionId    = permission.ViewOwnUserPermissionId,
            ViewAnyUserPermissionId    = permission.ViewAnyUserPermissionId,
            ViewPublicUserPermissionId = permission.ViewPublicUserPermissionId,
            ViewUserPermissionId       = permission.ViewUserPermissionId,

            UserId        = parameters.UserId,
            RelatedUserId = parameters.RelatedUserId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.RevokePermissionsToUser>(queryPermissions, queryPermissionsParameters);

        // [User Information]

        const string queryUserInformations = @"
SELECT 
    [U].[private]                                    AS [Private], 
    (CASE WHEN [U].[id] = @UserId THEN 1 ELSE 0 END) AS [Owner]
FROM [users] [U] WHERE [U].[id] = @RelatedUserId";

        var queryUserInformationParameters = new
        {
            RelatedUserId = parameters.RelatedUserId,
            UserId        = parameters.UserId
        };

        var userInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner)>(queryUserInformations, queryUserInformationParameters);

        // [VIEW]
        if (((userInformationResult.Private.HasValue && userInformationResult.Private.Value) && !permissionsResult.HasViewPublicUserPermission) &&
            ((userInformationResult.Owner.HasValue   && userInformationResult.Owner.Value)   && !permissionsResult.HasViewOwnUserPermission)    &&
            !permissionsResult.HasViewUserPermission &&
            !permissionsResult.HasViewAnyUserPermission)
        {
            resultConstructor.SetConstructor(new UserNotFoundError());

            return resultConstructor.Build();
        }

        // [GRANT_PERMISSIONS]
        if (((userInformationResult.Owner.HasValue && userInformationResult.Owner.Value) && !permissionsResult.HasRevokePermissionsOwnUserPermission) &&
            !permissionsResult.HasRevokePermissionsUserPermission &&
            !permissionsResult.HasRevokePermissionsAnyUserPermission)
        {
            resultConstructor.SetConstructor(new RevokePermissionsToUserDeniedError());

            return resultConstructor.Build();
        }
        // [Permission Objects Validations]

        var distinctPermission = parameters.Permissions.Distinct();

        var internalValues = new InternalValues.RevokePermissionsToUser()
        {
            Data = new()
            {
                Items = distinctPermission
                    .Distinct()
                    .ToDictionary(
                        x => x.Id,
                        x => new InternalValues.RevokePermissionsToUser.DataPropreties.Item()
                        {
                            Id = x.Id,

                            // [From parameters]

                            UserId       = x.UserId,
                            PermissionId = x.PermissionId,
                            RoleId       = x.RoleId,
                            AttributeId  = x.AttributeId
                        })
            }
        };

        // [User Id]
        var userIdResultDictionary = await ValidateUserId(
            internalValues.Data.Items.Values
                .Select(x => x.UserId)
                .Distinct(), 
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!userIdResultDictionary.TryGetValue(item.UserId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Role Id]
        var roleIdResultDictionary = await ValidateRoleId(
            internalValues.Data.Items.Values
                .Select(x => x.RoleId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!roleIdResultDictionary.TryGetValue(item.RoleId, out var result))
                continue;

            if(result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Role Id]
        var attributeIdResultDictionary = await ValidateRoleId(
            internalValues.Data.Items.Values.Where(x => x.AttributeId.HasValue)
                .Select(x => x.AttributeId!.Value)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values.Where(x => x.AttributeId.HasValue))
        {
            if (!attributeIdResultDictionary.TryGetValue(item.AttributeId!.Value, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        // [Permission Id]
        var permissionIdResultDictionary = await ValidatePermissionId(
            internalValues.Data.Items.Values
                .Select(x => x.PermissionId)
                .Distinct(),
            contextualizer);

        foreach (var item in internalValues.Data.Items.Values)
        {
            if (!permissionIdResultDictionary.TryGetValue(item.PermissionId, out var result))
                continue;

            if (result.IsFinished)
            {
                internalValues.Data.Items[item.Id].Result = result;

                internalValues.Data.Finish(item.Id);
            }
        }

        const string queryAllowedPermissions = @"
SELECT [P].[id] AS [Id] FROM [Permissions] WHERE [P].[name] IN 
('GRANT_PERMISSIONS_USER', 'REVOKE_PERMISSIONS_USER', 'GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER', 'REVOKE_PERMISSIONS_USER', 'CHAT_USER', 'VIEW_USER', 'VIEW_LAWYER_ACCOUNT_USER', 'VIEW_CUSTOMER_ACCOUNT_USER', 'VIEW_PERMISSIONS_USER', 'VIEW_PERMISSIONS_LAWYER_ACCOUNT_USER', 'VIEW_PERMISSIONS_CUSTOMER_ACCOUNT_USER', 'EDIT_USER');";

        var allowedPermissions = await connection.Connection.QueryAsync<int>(queryAllowedPermissions);

        foreach (var item in internalValues.Data.Items.Values)
        {
            var resultContructor = new ResultConstructor();

            if (!allowedPermissions.Contains(item.PermissionId))
            {
                resultContructor.SetConstructor(new ForbiddenPermissionsToRevokeToUserError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }

            var hasPermissionToAssignUser = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    // [NOT ACL]

                    HasRevokePermissionsAnyUserPermission = permissionsResult.HasRevokePermissionsAnyUserPermission,

                    HasViewAnyUserPermission    = permissionsResult.HasViewAnyUserPermission,
                    HasViewPublicUserPermission = permissionsResult.HasViewPublicUserPermission,
                    
                    // [ACL]

                    ViewUserPermissionId = permission.ViewUserPermissionId,

                    RevokePermissionsUserPermissionId = permission.RevokePermissionsUserPermissionId,

                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId
                };

                var queryText = @$"
WITH [view_permission] AS (
    SELECT
        CASE
            WHEN

                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasViewOwnUserPermission OR @HasViewAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [VIEW_USER]

                    ([U].[private] = 1 AND (
                        @ViewUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1 FROM [permission_grants_relationship] [PGRu]
                            WHERE
                                [PGRu].[related_user_id] = @UserId               AND 
                                [PGRu].[user_id]         = @ExternalUserId       AND 
                                [PGRu].[role_id]         = @RoleId               AND 
                                [PGRu].[permission_id]   = @ViewUserPermissionId
                        )
                        OR @HasViewAnyUserPermission = 1
                    ))

                    OR 

                    -- [Layer 3: Public]

                    ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customer] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
),
[revoke_permission] AS (
    SELECT
        CASE
            WHEN
                -- [Block 1: User Level]

                (
                    -- [Layer 1: Ownership]

                    ([U].[id] = @ExternalUserId AND (@HasRevokePermissionsOwnUserPermission OR @HasRevokePermissionsAnyUserPermission = 1))

                    OR

                    -- [Layer 2: permission_grants_relationship (ACL Grant)] [REVOKE_PERMISSIONS_USER]

                    (@RevokePermissionsUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM [permission_grants_relationship] [PGRu2]
                        WHERE
                            [PGRu2].[related_user_id] = @UserId                            AND 
                            [PGRu2].[user_id]         = @ExternalUserId                    AND 
                            [PGRu2].[role_id]         = @RoleId                            AND 
                            [PGRu2].[permission_id]   = @RevokePermissionsUserPermissionId
                    )
                    OR @HasRevokePermissionsAnyUserPermission = 1)   
                )
            THEN 1 ELSE 0
        END AS [apply]
    FROM [users] [U]
    LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
    LEFT JOIN [customer] [C] ON [C].[user_id] = [U].[id]
    WHERE [U].[id] = @UserId
)
SELECT
    CASE WHEN [view_permission].[apply] = 1 AND [revoke_permission].[apply] = 1 THEN 1 
    ELSE 0 END AS [result]
FROM [view_permission], [revoke_permission];";

                    var result = await connection.Connection.QueryFirstAsync<bool>(
                      new CommandDefinition(
                              commandText:       queryText,
                              parameters:        queryParameters,
                              transaction:       connection.Transaction,
                              cancellationToken: contextualizer.CancellationToken,
                              commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

                    return result;

            });

            if (!hasPermissionToAssignUser)
            {
                resultContructor.SetConstructor(new RevokePermissionsToUserForSpecificUserDeniedError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }
        }

        if (!internalValues.Data.Items.Any())
        {
            resultConstructor.SetConstructor(new RevokePermissionsToUserSuccess()
            {
                Details = new()
                {
                    DeletedItems = 0,

                    Result = internalValues.Data.Finished.Values.Select((item) =>
                        new RevokePermissionsToUserSuccess.DetailsVariation.Fields
                        {
                            UserId       = item.UserId,
                            PermissionId = item.PermissionId,
                            RoleId       = item.RoleId,
                            AttributeId  = item.AttributeId,

                            Result = item.Result
                        })
                }
            });

            return resultConstructor.Build();
        }
          
        var deletedItems = await ValuesExtensions.GetValue(async () =>
        {
            var dynamicParameter = new DynamicParameters();

            dynamicParameter.Add($"RelatedUserId", parameters.RelatedUserId);

            var stringBuilder = new StringBuilder();

            foreach (var (item, index) in internalValues.Data.Items.Values.Select((item, index) => (item, index)))
            {
                dynamicParameter.Add($"PermissionId({index})", item.PermissionId);
                dynamicParameter.Add($"UserId({index})", item.UserId);
                dynamicParameter.Add($"RoleId({index})", item.RoleId);
                dynamicParameter.Add($"AttributeId({index})", item.AttributeId);

                stringBuilder.Append($"DELETE [permission_grants_relationship] WHERE [related_user_id] = @RelatedUserId AND [user_id] = @UserId({index}) AND [permission_id] = @PermissionId({index}) AND [user_id] = @UserId({index}) AND [attribute_id] = @AttributeId({index});");
            }

            var deletedItems = await connection.Connection.ExecuteAsync(stringBuilder.ToString(), dynamicParameter);

            return deletedItems;
        });

        resultConstructor.SetConstructor(new RevokePermissionsToUserSuccess()
        {
            Details = new()
            {
                DeletedItems = deletedItems,

                Result = internalValues.Data.Finished.Values.Select((item) =>
                    new RevokePermissionsToUserSuccess.DetailsVariation.Fields
                    {
                        UserId       = item.UserId,
                        PermissionId = item.PermissionId,
                        RoleId       = item.RoleId,
                        AttributeId  = item.AttributeId,

                        Result = item.Result
                    })
            }
        });

        return resultConstructor.Build();
    }

    #endregion

    #endregion

    #region SearchEnabledUsersToGrantPermissionsAsync

    public async Task<Result<SearchEnabledUsersToGrantPermissionsInformation>> SearchEnabledUsersToGrantPermissionsAsync(SearchEnabledUsersToGrantPermissionsParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<SearchEnabledUsersToGrantPermissionsInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<SearchEnabledUsersToGrantPermissionsInformation>().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<SearchEnabledUsersToGrantPermissionsInformation>().Incorporate(roleIdResult);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            GrantPermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_USER, contextualizer),
            GrantPermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            GrantPermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_USER, contextualizer),
            GrantPermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            GrantPermissionsAnyUserPermissionId            = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_USER, contextualizer),
            GrantPermissionsAnyLawyerAccountPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsAnyCustomerAccountPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
        ('HasViewOwnUserPermission',                            @ViewOwnUserPermissionId),
        ('HasViewAnyUserPermission',                            @ViewAnyUserPermissionId),
        ('HasViewPublicUserPermission',                         @ViewPublicUserPermissionId),
        ('HasViewOwnLawyerAccountUserPermission',               @ViewOwnLawyerAccountUserPermissionId),
        ('HasViewAnyLawyerAccountUserPermission',               @ViewAnyLawyerAccountUserPermissionId),
        ('HasViewPublicLawyerAccountUserPermission',            @ViewPublicLawyerAccountUserPermissionId),
        ('HasViewOwnCustomerAccountUserPermission',             @ViewOwnCustomerAccountUserPermissionId),
        ('HasViewAnyCustomerAccountUserPermission',             @ViewAnyCustomerAccountUserPermissionId),
        ('HasViewPublicCustomerAccountUserPermission',          @ViewPublicCustomerAccountUserPermissionId),

        ('HasGrantPermissionsOwnUserPermission',                 @GrantPermissionsOwnUserPermissionId),
        ('HasGrantPermissionsAnyUserPermission',                 @GrantPermissionsAnyUserPermissionId),
        ('HasGrantPermissionsOwnLawyerAccountUserPermission',    @GrantPermissionsOwnLawyerAccountUserPermissionId),
        ('HasGrantPermissionsAnyLawyerAccountUserPermission',    @GrantPermissionsAnyLawyerAccountUserPermissionId),
        ('HasGrantPermissionsOwnCustomerAccountUserPermission',  @GrantPermissionsOwnCustomerAccountUserPermissionId),
        ('HasGrantPermissionsAnyCustomerAccountUserPermission',  @GrantPermissionsAnyCustomerAccountUserPermissionId)
),
[grants] AS (
    SELECT
        [PC].[permission_name],
        [PGU].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId
    UNION
    SELECT
        [PC].[permission_name],
        [PG].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND 
         [PG].[role_id]       = @RoleId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'      THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'      THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCustomerAccountUserPermission],

    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsOwnCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasGrantPermissionsAnyCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasGrantPermissionsAnyCustomerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,               
                ViewOwnLawyerAccountUserPermissionId   = permission.ViewOwnLawyerAccountUserPermissionId,            
                ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

                ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
                ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,            
                ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

                ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
                ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
                ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

                GrantPermissionsOwnUserPermissionId                = permission.GrantPermissionsOwnUserPermissionId,
                GrantPermissionsOwnLawyerAccountUserPermissionId   = permission.GrantPermissionsOwnLawyerAccountUserPermissionId,
                GrantPermissionsOwnCustomerAccountUserPermissionId = permission.GrantPermissionsOwnCustomerAccountUserPermissionId,

                GrantPermissionsAnyUserPermissionId            = permission.GrantPermissionsAnyUserPermissionId,
                GrantPermissionsAnyLawyerAccountPermissionId   = permission.GrantPermissionsAnyLawyerAccountPermissionId,
                GrantPermissionsAnyCustomerAccountPermissionId = permission.GrantPermissionsAnyCustomerAccountPermissionId,

                UserId = parameters.UserId,
                RoleId = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.SearchEnabledUsersToGrantPermissions>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                // [NOT ACL]

                HasViewOwnUserPermission                = permissionsResult.HasViewOwnUserPermission,
                HasViewOwnLawyerAccountUserPermission   = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                HasViewOwnCustomerAccountUserPermission = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                
                HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                HasViewAnyLawyerAccountUserPermission   = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                HasViewAnyCustomerAccountUserPermission = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                
                HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                HasViewPublicLawyerAccountUserPermission   = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,

                HasGrantPermissionsOwnUserPermission                = permissionsResult.HasGrantPermissionsOwnUserPermission,
                HasGrantPermissionsOwnLawyerAccountUserPermission   = permissionsResult.HasGrantPermissionsOwnLawyerAccountUserPermission,
                HasGrantPermissionsOwnCustomerAccountUserPermission = permissionsResult.HasGrantPermissionsOwnCustomerAccountUserPermission,

                HasGrantPermissionsAnyUserPermission                = permissionsResult.HasGrantPermissionsAnyUserPermission,
                HasGrantPermissionsAnyLawyerAccountUserPermission   = permissionsResult.HasGrantPermissionsAnyLawyerAccountUserPermission,
                HasGrantPermissionsAnyCustomerAccountUserPermission = permissionsResult.HasGrantPermissionsAnyCustomerAccountUserPermission,

                // [ACL]
                
                ViewUserPermissionId                = permission.ViewUserPermissionId,
                ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,

                GrantPermissionsUserPermissionId                = permission.GrantPermissionsUserPermissionId,
                GrantPermissionsLawyerAccountUserPermissionId   = permission.GrantPermissionsLawyerAccountUserPermissionId,
                GrantPermissionsCustomerAccountUserPermissionId = permission.GrantPermissionsCustomerAccountUserPermissionId,

                UserId = parameters.UserId,
                RoleId = parameters.RoleId
            };

            var queryText = $@"
SELECT
    [U].[id],
    [U].[name],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId
                    )
                    OR @HasViewAnyUserPermission = 1
                ))
                OR ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [L].[private] = 1 AND (
                        @ViewLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1
                            FROM [permission_grants_relationship] [PGRl]
                            WHERE
                                [PGRl].[related_user_id] = @UserId                            AND 
                                [PGRl].[user_id]         = @ExternalUserId                    AND 
                                [PGRl].[role_id]         = @RoleId                            AND 
                                [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId
                        )
                        OR @HasViewAnyLawyerAccountUserPermission = 1
                    ) THEN 1
                    WHEN [L].[private] = 0 AND (
                        @HasViewPublicLawyerAccountUserPermission = 1
                        OR @HasViewAnyLawyerAccountUserPermission = 1
                    ) THEN 1
                    ELSE 0
                END = 1
            )
        THEN 1
        ELSE 0
    END AS [HasLawyerAccount],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId
                    )
                    OR @HasViewAnyUserPermission = 1
                ))
                OR ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [C].[private] = 1 AND (
                        @ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS  (
                            SELECT 1
                            FROM [permission_grants_relationship] [PGRc]
                            WHERE
                                [PGRc].[related_user_id] = @UserId                              AND 
                                [PGRc].[user_id]         = @ExternalUserId                      AND 
                                [PGRc].[role_id]         = @RoleId                              AND 
                                [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId
                        )
                        OR @HasViewAnyCustomerAccountUserPermission = 1
                    ) THEN 1
                    WHEN [C].[private] = 0 AND (
                        @HasViewPublicCustomerAccountUserPermission = 1
                        OR @HasViewAnyCustomerAccountUserPermission = 1
                    ) THEN 1
                    ELSE 0
                END = 1
            )
        THEN 1
        ELSE 0
    END AS [HasCustomerAccount],

    SELECT 
    CASE 
        WHEN (
            -- [Layer 1: Ownership]

            ([U].[id] = @UserId AND @HasGrantPermissionsOwnUserPermission = 1)

            OR

            -- [Layer 2: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSIONS_USER]

            (@GrantPermissionsUserPermissionId IS NOT NULL AND EXISTS (
                SELECT 1 FROM [permission_grants_relationship] [PGR]
                WHERE 
                    [PGC].[related_user_id] = [U].[id]                          AND
                    [PGC].[user_id]         = @UserId                           AND
                    [PGC].[permission_id]   = @GrantPermissionsUserPermissionId AND
                    [PGC].[role_id]         = @RoleId
            ))

            OR

            -- [Layer 3: permission_grants (Role Grant) | permission_grants_user (User Grant)] [GRANT_PERMISSIONS_ANY_USER]

            @HasGrantPermissionsAnyUserPermission = 1

        ) THEN 1

        ELSE 0
    END AS [CanBeGrantAsUser],

    SELECT 
    CASE 
        WHEN (

            [L].[id] IS NOT NULL

            AND (

            -- [Layer 1: Ownership]

            ([L].[user_id] = @UserId AND @HasGrantPermissionsOwnLawyerAccountUserPermission = 1)

            OR

            -- [Layer 2: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER]

            (@GrantPermissionsLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                SELECT 1 FROM [permission_grants_relationship] [PGR]
                WHERE 
                    [PGR].[related_user_id] = [U].[id]                                       AND
                    [PGR].[user_id]         = @UserId                                        AND
                    [PGR].[permission_id]   = @GrantPermissionsLawyerAccountUserPermissionId AND
                    [PGR].[role_id]         = @RoleId
            ))

            OR

            -- [Layer 3: permission_grants (Role Grant) | permission_grants_user (User Grant)] [GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

            @HasGrantPermissionsAnyLawyerAccountUserPermission = 1

        ) THEN 1

        ELSE 0
    END AS [CanBeGrantAsLawyer],

    SELECT 
    CASE 
        WHEN (

            [C].[id] IS NOT NULL

            AND (

            -- [Layer 1: Ownership]

            ([C].[user_id] = @UserId AND @HasGrantPermissionsOwnCustomerAccountUserPermission = 1)

            OR

            -- [Layer 2: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER]

            (@GrantPermissionsCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                SELECT 1 FROM [permission_grants_relationship] [PGR]
                WHERE 
                    [PGR].[related_user_id] = [U].[id]                                         AND
                    [PGR].[user_id]         = @UserId                                          AND
                    [PGR].[permission_id]   = @GrantPermissionsCustomerAccountUserPermissionId AND
                    [PGR].[role_id]         = @RoleId
            ))

            OR

            -- [Layer 3: permission_grants (Role Grant) | permission_grants_user (User Grant)] [GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER]

            @HasGrantPermissionsAnyCustomerAccountUserPermission = 1

        ) THEN 1

        ELSE 0
    END AS [CanBeGrantAsCustomer]
FROM [users] [U]
LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
LEFT JOIN [customers] [C] ON [C].[user_id] = [U].[id]

WHERE
    (@NameFilter IS NULL OR [U].[name] LIKE @NameFilter)
    AND (

        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_USER | VIEW_USER]

        ([U].[id] = @UserId AND @HasViewOwnUserPermission = 1)

        OR

        (@ViewUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            WHERE
                [PGR].[related_user_id] = [U].[id]            AND
                [PGR].[user_id]       = @UserId               AND
                [PGR].[permission_id] = @ViewUserPermissionId AND
                [PGR].[role_id]       = @RoleId
        ))

        OR

        @HasViewAnyUserPermission = 1

        OR

        -- [Block 2: User is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicUserPermission = 1
            OR @HasViewAnyUserPermission = 1
        ))
    )

ORDER BY [U].[id] DESC
LIMIT @Limit OFFSET @Offset;";

            SearchEnabledUsersToGrantPermissionsInformation information;

            using (var multiple = await connection.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds
                    )))
            {
                information = new SearchEnabledUsersToGrantPermissionsInformation
                {
                    Items = await multiple.ReadAsync<SearchEnabledUsersToGrantPermissionsInformation.ItemProperties>()
                };
            }

            return information;
        });

        return resultConstructor.Build<SearchEnabledUsersToGrantPermissionsInformation>(information);
    }

    #endregion

    #region SearchEnabledUsersToRevokePermissionsAsync

    public async Task<Result<SearchEnabledUsersToRevokePermissionsInformation>> SearchEnabledUsersToRevokePermissionsAsync(SearchEnabledUsersToRevokePermissionsParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<SearchEnabledUsersToRevokePermissionsInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        // [Principal Object Validations]

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build<SearchEnabledUsersToRevokePermissionsInformation>().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<SearchEnabledUsersToRevokePermissionsInformation>().Incorporate(roleIdResult);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            RevokePermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_USER, contextualizer),
            RevokePermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            RevokePermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_USER, contextualizer),
            RevokePermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            RevokePermissionsAnyUserPermissionId            = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
            RevokePermissionsAnyLawyerAccountPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsAnyCustomerAccountPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
        ('HasViewOwnUserPermission',                            @ViewOwnUserPermissionId),
        ('HasViewAnyUserPermission',                            @ViewAnyUserPermissionId),
        ('HasViewPublicUserPermission',                         @ViewPublicUserPermissionId),
        ('HasViewOwnLawyerAccountUserPermission',               @ViewOwnLawyerAccountUserPermissionId),
        ('HasViewAnyLawyerAccountUserPermission',               @ViewAnyLawyerAccountUserPermissionId),
        ('HasViewPublicLawyerAccountUserPermission',            @ViewPublicLawyerAccountUserPermissionId),
        ('HasViewOwnCustomerAccountUserPermission',             @ViewOwnCustomerAccountUserPermissionId),
        ('HasViewAnyCustomerAccountUserPermission',             @ViewAnyCustomerAccountUserPermissionId),
        ('HasViewPublicCustomerAccountUserPermission',          @ViewPublicCustomerAccountUserPermissionId),

        ('HasRevokePermissionsOwnUserPermission',                 @RevokePermissionsOwnUserPermissionId),
        ('HasRevokePermissionsAnyUserPermission',                 @RevokePermissionsAnyUserPermissionId),
        ('HasRevokePermissionsOwnLawyerAccountUserPermission',    @RevokePermissionsOwnLawyerAccountUserPermissionId),
        ('HasRevokePermissionsAnyLawyerAccountUserPermission',    @RevokePermissionsAnyLawyerAccountUserPermissionId),
        ('HasRevokePermissionsOwnCustomerAccountUserPermission',  @RevokePermissionsOwnCustomerAccountUserPermissionId),
        ('HasRevokePermissionsAnyCustomerAccountUserPermission',  @RevokePermissionsAnyCustomerAccountUserPermissionId)
),
[grants] AS (
    SELECT
        [PC].[permission_name],
        [PGU].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_user] [PGU]
      ON [PGU].[permission_id] = [PC].[permission_id] AND 
         [PGU].[user_id]       = @UserId              AND 
         [PGU].[role_id]       = @RoleId
    UNION
    SELECT
        [PC].[permission_name],
        [PG].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants] [PG]
      ON [PG].[permission_id] = [PC].[permission_id] AND 
         [PG].[role_id]       = @RoleId
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'      THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'      THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'    THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCustomerAccountUserPermission],

    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyUserPermission'                THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyLawyerAccountUserPermission'   THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsOwnCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasRevokePermissionsAnyCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRevokePermissionsAnyCustomerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];"";";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,               
                ViewOwnLawyerAccountUserPermissionId   = permission.ViewOwnLawyerAccountUserPermissionId,            
                ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

                ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
                ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,            
                ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

                ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
                ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
                ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

                RevokePermissionsOwnUserPermissionId                = permission.RevokePermissionsOwnUserPermissionId,
                RevokePermissionsOwnLawyerAccountUserPermissionId   = permission.RevokePermissionsOwnLawyerAccountUserPermissionId,
                RevokePermissionsOwnCustomerAccountUserPermissionId = permission.RevokePermissionsOwnCustomerAccountUserPermissionId,

                RevokePermissionsAnyUserPermissionId            = permission.RevokePermissionsAnyUserPermissionId,
                RevokePermissionsAnyLawyerAccountPermissionId   = permission.RevokePermissionsAnyLawyerAccountPermissionId,
                RevokePermissionsAnyCustomerAccountPermissionId = permission.RevokePermissionsAnyCustomerAccountPermissionId,

                UserId = parameters.UserId,
                RoleId = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.SearchEnabledUsersToRevokePermissions>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                // [NOT ACL]

                HasViewOwnUserPermission                = permissionsResult.HasViewOwnUserPermission,
                HasViewOwnLawyerAccountUserPermission   = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                HasViewOwnCustomerAccountUserPermission = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                
                HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                HasViewAnyLawyerAccountUserPermission   = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                HasViewAnyCustomerAccountUserPermission = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                
                HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                HasViewPublicLawyerAccountUserPermission   = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,

                HasRevokePermissionsOwnUserPermission                = permissionsResult.HasRevokePermissionsOwnUserPermission,
                HasRevokePermissionsOwnLawyerAccountUserPermission   = permissionsResult.HasRevokePermissionsOwnLawyerAccountUserPermission,
                HasRevokePermissionsOwnCustomerAccountUserPermission = permissionsResult.HasRevokePermissionsOwnCustomerAccountUserPermission,

                HasRevokePermissionsAnyUserPermission                = permissionsResult.HasRevokePermissionsAnyUserPermission,
                HasRevokePermissionsAnyLawyerAccountUserPermission   = permissionsResult.HasRevokePermissionsAnyLawyerAccountUserPermission,
                HasRevokePermissionsAnyCustomerAccountUserPermission = permissionsResult.HasRevokePermissionsAnyCustomerAccountUserPermission,

                // [ACL]
                
                ViewUserPermissionId                = permission.ViewUserPermissionId,
                ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,

                RevokePermissionsUserPermissionId                = permission.RevokePermissionsUserPermissionId,
                RevokePermissionsLawyerAccountUserPermissionId   = permission.RevokePermissionsLawyerAccountUserPermissionId,
                RevokePermissionsCustomerAccountUserPermissionId = permission.RevokePermissionsCustomerAccountUserPermissionId,

                UserId = parameters.UserId,
                RoleId = parameters.RoleId
            };

            var queryText = $@"
SELECT
    [U].[id],
    [U].[name],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId
                    )
                    OR @HasViewAnyUserPermission = 1
                ))
                OR ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [L].[private] = 1 AND (
                        @ViewLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1
                            FROM [permission_grants_relationship] [PGRl]
                            WHERE
                                [PGRl].[related_user_id] = @UserId                            AND 
                                [PGRl].[user_id]         = @ExternalUserId                    AND 
                                [PGRl].[role_id]         = @RoleId                            AND 
                                [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId
                        )
                        OR @HasViewAnyLawyerAccountUserPermission = 1
                    ) THEN 1
                    WHEN [L].[private] = 0 AND (
                        @HasViewPublicLawyerAccountUserPermission = 1
                        OR @HasViewAnyLawyerAccountUserPermission = 1
                    ) THEN 1
                    ELSE 0
                END = 1
            )
        THEN 1
        ELSE 0
    END AS [HasLawyerAccount],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId
                    )
                    OR @HasViewAnyUserPermission = 1
                ))
                OR ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [C].[private] = 1 AND (
                        @ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS  (
                            SELECT 1
                            FROM [permission_grants_relationship] [PGRc]
                            WHERE
                                [PGRc].[related_user_id] = @UserId                              AND 
                                [PGRc].[user_id]         = @ExternalUserId                      AND 
                                [PGRc].[role_id]         = @RoleId                              AND 
                                [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId
                        )
                        OR @HasViewAnyCustomerAccountUserPermission = 1
                    ) THEN 1
                    WHEN [C].[private] = 0 AND (
                        @HasViewPublicCustomerAccountUserPermission = 1
                        OR @HasViewAnyCustomerAccountUserPermission = 1
                    ) THEN 1
                    ELSE 0
                END = 1
            )
        THEN 1
        ELSE 0
    END AS [HasCustomerAccount],

    SELECT 
    CASE 
        WHEN (
            -- [Layer 1: Ownership]

            ([U].[id] = @UserId AND @HasRevokePermissionsOwnUserPermission = 1)

            OR

            -- [Layer 2: permission_grants_relationship (ACL Grant)] [REVOKE_PERMISSIONS_USER]

            (@RevokePermissionsUserPermissionId IS NOT NULL AND EXISTS (
                SELECT 1 FROM [permission_grants_relationship] [PGR]
                WHERE 
                    [PGC].[related_user_id] = [U].[id]                           AND
                    [PGC].[user_id]         = @UserId                            AND
                    [PGC].[permission_id]   = @RevokePermissionsUserPermissionId AND
                    [PGC].[role_id]         = @RoleId
            ))

            OR

            -- [Layer 3: permission_grants (Role Grant) | permission_grants_user (User Grant)] [REVOKE_PERMISSIONS_ANY_USER]

            @HasRevokePermissionsAnyUserPermission = 1

        ) THEN 1

        ELSE 0
    END AS [CanBeRevokeAsUser],

    SELECT 
    CASE 
        WHEN (

            [L].[id] IS NOT NULL

            AND (

            -- [Layer 1: Ownership]

            ([L].[user_id] = @UserId AND @HasRevokePermissionsOwnLawyerAccountUserPermission = 1)

            OR

            -- [Layer 2: permission_grants_relationship (ACL Grant)] [REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER]

            (@RevokePermissionsLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
                SELECT 1 FROM [permission_grants_relationship] [PGR]
                WHERE 
                    [PGR].[related_user_id] = [U].[id]                                        AND
                    [PGR].[user_id]         = @UserId                                         AND
                    [PGR].[permission_id]   = @RevokePermissionsLawyerAccountUserPermissionId AND
                    [PGR].[role_id]         = @RoleId
            ))

            OR

            -- [Layer 3: permission_grants (Role Grant) | permission_grants_user (User Grant)] [REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

            @HasRevokePermissionsAnyLawyerAccountUserPermission = 1

        ) THEN 1

        ELSE 0
    END AS [CanBeRevokeAsLawyer],

    SELECT 
    CASE 
        WHEN (

            [C].[id] IS NOT NULL

            AND (

            -- [Layer 1: Ownership]

            ([C].[user_id] = @UserId AND @HasRevokePermissionsOwnCustomerAccountUserPermission = 1)

            OR

            -- [Layer 2: permission_grants_relationship (ACL Grant)] [REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER]

            (@RevokePermissionsCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                SELECT 1 FROM [permission_grants_relationship] [PGR]
                WHERE 
                    [PGR].[related_user_id] = [U].[id]                                          AND
                    [PGR].[user_id]         = @UserId                                           AND
                    [PGR].[permission_id]   = @RevokePermissionsCustomerAccountUserPermissionId AND
                    [PGR].[role_id]         = @RoleId
            ))

            OR

            -- [Layer 3: permission_grants (Role Grant) | permission_grants_user (User Grant)] [REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER]

            @HasRevokePermissionsAnyCustomerAccountUserPermission = 1

        ) THEN 1

        ELSE 0
    END AS [CanBeRevokeAsCustomer]
FROM [users] [U]
LEFT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
LEFT JOIN [customers] [C] ON [C].[user_id] = [U].[id]

WHERE
    (@NameFilter IS NULL OR [U].[name] LIKE @NameFilter)
    AND (

        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_USER | VIEW_USER]

        ([U].[id] = @UserId AND @HasViewOwnUserPermission = 1)

        OR

        (@ViewUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            WHERE
                [PGR].[related_user_id] = [U].[id]            AND
                [PGR].[user_id]       = @UserId               AND
                [PGR].[permission_id] = @ViewUserPermissionId AND
                [PGR].[role_id]       = @RoleId
        ))

        OR

        @HasViewAnyUserPermission = 1

        OR

        -- [Block 2: User is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicUserPermission = 1
            OR @HasViewAnyUserPermission = 1
        ))
    )

ORDER BY [U].[id] DESC
LIMIT @Limit OFFSET @Offset;";

            SearchEnabledUsersToRevokePermissionsInformation information;

            using (var multiple = await connection.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds
                    )))
            {
                information = new SearchEnabledUsersToRevokePermissionsInformation
                {
                    Items = await multiple.ReadAsync<SearchEnabledUsersToRevokePermissionsInformation.ItemProperties>()
                };
            }

            return information;
        });

        return resultConstructor.Build<SearchEnabledUsersToRevokePermissionsInformation>(information);
    }

    #endregion

    #region Validations

    private async Task<Dictionary<int, Result>> ValidatePermissionId(
        IEnumerable<int> list,
        Contextualizer contextualizer)
    {
        var distinctList = list.Distinct().ToList();
        if (!distinctList.Any())
            return new Dictionary<int, Result>();

        var result = new Dictionary<int, Result>();

        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { Permissions = list };

        var queryText = "SELECT [P].[id] FROM [permissions] P WHERE [P].[id] IN @Permissions";
        
        var results = await connection.Connection.QueryAsync<int>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        foreach (var item in distinctList)
        {
            var resultContructor = new ResultConstructor();

            var match = results.FirstOrDefault(r => r == item);

            if (match == 0)
            {
                resultContructor.SetConstructor(new PermissionNotFoundError());

                result.Add(item, resultContructor.Build());
            }
        }
        return result;
    }

    private async Task<Result> ValidatePermissionId(
        int id,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { PermissionId = id };

        var queryText = "SELECT [P].[id] FROM [permission] P WHERE [P].[id] = @PermissionId";
        
        var result = await connection.Connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var resultContructor = new ResultConstructor();

        if (result == null)
        {
            resultContructor.SetConstructor(new PermissionNotFoundError());

            return resultContructor.Build();
        }
        return resultContructor.Build();
    }

    private async Task<Dictionary<int, Result>> ValidateAttributeId(
        IEnumerable<int> list,
        Contextualizer contextualizer)
    {
        var distinctList = list.Distinct().ToList();
        if (!distinctList.Any())
            return new Dictionary<int, Result>();

        var result = new Dictionary<int, Result>();

        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { Attributes = list };

        var queryText = "SELECT [A].[id] FROM [attributes] A WHERE [A].[id] IN @Attributes";
        
        var results = await connection.Connection.QueryAsync<int>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        foreach (var item in distinctList)
        {
            var resultContructor = new ResultConstructor();

            var match = results.FirstOrDefault(r => r == item);

            if (match == 0)
            {
                resultContructor.SetConstructor(new RoleNotFoundError());

                result.Add(item, resultContructor.Build());
            }
            result.Add(item, resultContructor.Build());
        }
        return result;
    }

    private async Task<Result> ValidateAttributeId(
        int id,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { AttributeId = id };

        var queryText = "SELECT [A].[id] FROM [attributes] A WHERE [A].[id] = @AttributeId";
        
        var result = await connection.Connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var resultContructor = new ResultConstructor();

        if (result == null)
        {
            resultContructor.SetConstructor(new PermissionNotFoundError());

            return resultContructor.Build();
        }
        return resultContructor.Build();
    }

    private async Task<Dictionary<int, Result>> ValidateRoleId(
        IEnumerable<int> list,
        Contextualizer contextualizer)
    {
        var distinctList = list.Distinct().ToList();
        if (!distinctList.Any())
            return new Dictionary<int, Result>();

        var result = new Dictionary<int, Result>();

        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { Roles = list };

        var queryText = "SELECT [R].[id] FROM [roles] R WHERE [R].[id] IN @Roles";
        
        var results = await connection.Connection.QueryAsync<int>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        foreach (var item in distinctList)
        {
            var resultContructor = new ResultConstructor();

            var match = results.FirstOrDefault(r => r == item);

            if (match == 0)
            {
                resultContructor.SetConstructor(new RoleNotFoundError());

                result.Add(item, resultContructor.Build());
            }
            result.Add(item, resultContructor.Build());
        }

        return result;
    }

    private async Task<Result> ValidateRoleId(
        int id,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { RoleId = id };

        var queryText = "SELECT [R].[id] FROM [roles] R WHERE [R].[id] = @RoleId";
        
        var result = await connection.Connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var resultContructor = new ResultConstructor();

        if (result == null)
        {
            resultContructor.SetConstructor(new RoleNotFoundError());

            return resultContructor.Build();
        }
        return resultContructor.Build();
    }

    private async Task<Dictionary<int, Result>> ValidateUserId(
        IEnumerable<int> list,
        Contextualizer contextualizer)
    {
        var distinctList = list.Distinct().ToList();
        if (!distinctList.Any())
            return new Dictionary<int, Result>();

        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { Users = list };

        var queryText = "SELECT [U].[id] FROM [users] U WHERE [U].[id] IN @Users";
        
        var results = await connection.Connection.QueryAsync<int>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var result = new Dictionary<int, Result>();

        foreach (var item in distinctList)
        {
            var resultContructor = new ResultConstructor();

            var match = results.FirstOrDefault(r => r == item);

            if (match == 0)
            {
                resultContructor.SetConstructor(new UserNotFoundError());

                result.Add(item, resultContructor.Build());
            }
            result.Add(item, resultContructor.Build());
        }

        return result;
    }

    private async Task<Result> ValidateUserId(
        int id,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { UserId = id };

        var queryText = "SELECT [U].[id] FROM [users] U WHERE [U].[id] = @UserId";
        
        var result = await connection.Connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var resultContructor = new ResultConstructor();

        if (result == null)
        {
            resultContructor.SetConstructor(new UserNotFoundError());

            return resultContructor.Build();
        }
        return resultContructor.Build();
    }

    private async Task<Dictionary<int, Result>> ValidateCaseId(
        IEnumerable<int> list,
        Contextualizer contextualizer)
    {
        var distinctList = list.Distinct().ToList();
        if (!distinctList.Any())
            return new Dictionary<int, Result>();

        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { Cases = list };

        var queryText = "SELECT [C].[id] FROM [users] C WHERE [C].[id] IN @Cases";
        
        var results = await connection.Connection.QueryAsync<int>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var result = new Dictionary<int, Result>();

        foreach (var item in distinctList)
        {
            var resultContructor = new ResultConstructor();

            var match = results.FirstOrDefault(r => r == item);

            if (match == 0)
            {
                resultContructor.SetConstructor(new CaseNotFoundError());

                result.Add(item, resultContructor.Build());
            }
            result.Add(item, resultContructor.Build());
        }

        return result;
    }

    private async Task<Result> ValidateCaseId(
        int id,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { CaseId = id };

        var queryText = "SELECT [C].[id] FROM [cases] C WHERE [C].[id] = @CaseId";
        
        var result = await connection.Connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var resultContructor = new ResultConstructor();

        if (result == null)
        {
            resultContructor.SetConstructor(new CaseNotFoundError());

            return resultContructor.Build();
        }
        return resultContructor.Build();
    }

    private async Task<Dictionary<(int UserId, int AttributeId), Result>> ValidateAttributeAccount(
        IEnumerable<(int UserId, int AttributeId)> pairs,
        Contextualizer contextualizer)
    {
        var distinctPairs = pairs.Distinct().ToList();
        if (!distinctPairs.Any())
            return new Dictionary<(int, int), Result>();

        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var selectClauses = string.Join("\nUNION ALL\n", distinctPairs.Select((p, i) => $"SELECT @UserId{i} AS UserId, @AttributeId{i} AS AttributeId"));

        var queryParameters = new Dictionary<string, object>();
        for (int i = 0; i < distinctPairs.Count; i++)
        {
            queryParameters[$"UserId{i}"]      = distinctPairs[i].UserId;
            queryParameters[$"AttributeId{i}"] = distinctPairs[i].AttributeId;
        }

        var queryText = $@"
        SELECT 
            [P].[UserId],
            [P].[AttributeId],
            CASE 
                WHEN [A].[id] IS NULL OR [U].[id] IS NULL THEN NULL
                ELSE 
                    CASE 
                        WHEN UPPER([A].[name]) = 'LAWYER' THEN 
                            CASE WHEN EXISTS (SELECT 1 FROM [lawyers] [L] WHERE [L].[user_id] = [P].[UserId]) THEN 1 ELSE 0 END
                        WHEN UPPER([A].[name]) = 'CUSTOMER' THEN 
                            CASE WHEN EXISTS (SELECT 1 FROM [customers] [C] WHERE [C].[user_id] = [P].[UserId]) THEN 1 ELSE 0 END
                        ELSE 0
                    END
            END AS IsCapable
        FROM 
        (
            {selectClauses}
        ) AS [P]
        LEFT JOIN 
            [attributes] [A] ON [A].[id] = [P].[AttributeId]
        LEFT JOIN 
            [users] [U] ON [U].[id] = [P].[UserId]";

        var results = await connection.Connection.QueryAsync<(int UserId, int AttributeId, bool? IsCapable)>(
            new CommandDefinition(
                commandText:       queryText,
                parameters:        queryParameters,
                transaction:       connection.Transaction,
                cancellationToken: contextualizer.CancellationToken,
                commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var result = new Dictionary<(int, int), Result>();

        foreach (var pair in distinctPairs)
        {
            var resultConstructor = new ResultConstructor();

            var match = results.FirstOrDefault(r => r.UserId == pair.UserId && r.AttributeId == pair.AttributeId);

            if (match.UserId == 0)
            {
                resultConstructor.SetConstructor(new UserNotFoundError());

                result[pair] = resultConstructor.Build();
                continue;
            }

            if (match.IsCapable == null)
            {
                resultConstructor.SetConstructor(new UserNotCapableForAttributeAccountError());

                result[pair] = resultConstructor.Build();
                continue;
            }

            result[pair] = resultConstructor.Build();
        }
        return result;
    }

    private async Task<Result> ValidateAttributeAccount(
        int userId,
        int attributeId,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var queryParameters = new { UserId = userId, AttributeId = attributeId };

        var queryText = @"
        SELECT 
            CASE 
                WHEN [A].[id] IS NULL OR [U].[id] IS NULL THEN NULL
                ELSE [U].[id]
            END AS UserId,
            CASE 
                WHEN [A].[id] IS NULL OR [U].[id] IS NULL THEN NULL
                ELSE [A].[id]
            END AS AttributeId,
            CASE 
                WHEN [A].[id] IS NULL OR [U].[id] IS NULL THEN NULL
                ELSE 
                    CASE 
                        WHEN UPPER([A].[name]) = 'LAWYER' THEN 
                            CASE WHEN EXISTS (SELECT 1 FROM [lawyers] [L] WHERE [L].[user_id] = @UserId) THEN 1 ELSE 0 END
                        WHEN UPPER([A].[name]) = 'CUSTOMER' THEN 
                            CASE WHEN EXISTS (SELECT 1 FROM [customers] [C] WHERE [C].[user_id] = @UserId) THEN 1 ELSE 0 END
                        ELSE 0
                    END
            END AS IsCapable
        FROM 
            (SELECT [id], [name] FROM [attributes] WHERE [id] = @AttributeId) [A]
        FULL OUTER JOIN 
            (SELECT [id] FROM [users] WHERE [id] = @UserId) [U] ON 1=1";
        
        var result = await connection.Connection.QueryFirstAsync<(int UserId, int AttributeId, bool IsCapable)>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        var resultContructor = new ResultConstructor();

        if (!result.IsCapable)
        {
            resultContructor.SetConstructor(new UserNotCapableForAttributeAccountError());

            return resultContructor.Build();
        }
        return resultContructor.Build();
    }

    private async Task<int> GetPermissionIdAsync(
        string permissionName,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        if (string.IsNullOrWhiteSpace(permissionName)) return 0;

        var queryParameters = new { Name = permissionName };

        var queryText = "SELECT [P].[id] FROM [permissions] P WHERE [P].[name] = @Name LIMIT 1";
        
        var id = await connection.Connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
        
        return id ?? 0;
    } 

    #endregion   

}