using Dapper;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Lawyer.Common.Models;
using LawyerCustomerApp.Domain.Lawyer.Interfaces.Services;
using LawyerCustomerApp.Domain.Lawyer.Repositories.Models;
using LawyerCustomerApp.Domain.Lawyer.Responses.Repositories.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;

using PermissionSymbols = LawyerCustomerApp.External.Models.Permission.Permissions;

namespace LawyerCustomerApp.Domain.Lawyer.Repositories;

internal class Repository : IRepository
{
    private readonly IConfiguration _configuration;

    private readonly IHashService     _hashService;
    private readonly IDatabaseService _databaseService;
    public Repository(IConfiguration configuration, IHashService hashService, IDatabaseService databaseService)
    {
        _configuration = configuration;

        _hashService     = hashService;
        _databaseService = databaseService;
    }

    #region SearchAsync

    public async Task<Result<SearchInformation>> SearchAsync(SearchParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<SearchInformation>();
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
            return resultConstructor.Build<SearchInformation>().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build<SearchInformation>().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<SearchInformation>().Incorporate(roleIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build<SearchInformation>().Incorporate(attributeAccountResult);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]
            
            ViewUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewOwnUserPermission',                 @ViewOwnUserPermissionId),
    ('HasViewOwnLawyerAccountUserPermission',    @ViewOwnLawyerAccountUserPermissionId),
    ('HasViewPublicUserPermission',              @ViewPublicUserPermissionId),
    ('HasViewPublicLawyerAccountUserPermission', @ViewPublicLawyerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                 @ViewAnyUserPermissionId),
    ('HasViewAnyLawyerAccountUserPermission',    @ViewAnyLawyerAccountUserPermissionId)
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
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                 AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                 AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyLawyerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId              = permission.ViewOwnUserPermissionId,               
                ViewOwnLawyerAccountUserPermissionId = permission.ViewOwnLawyerAccountUserPermissionId,

                ViewPublicUserPermissionId              = permission.ViewPublicUserPermissionId,               
                ViewPublicLawyerAccountUserPermissionId = permission.ViewPublicLawyerAccountUserPermissionId,

                ViewAnyUserPermissionId              = permission.ViewAnyUserPermissionId,
                ViewAnyLawyerAccountUserPermissionId = permission.ViewAnyLawyerAccountUserPermissionId,

                AttributeId = parameters.UserId,
                UserId      = parameters.UserId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Search>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                // [NOT ACL]

                HasViewOwnUserPermission              = permissionsResult.HasViewOwnUserPermission,
                HasViewOwnLawyerAccountUserPermission = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                
                HasViewAnyUserPermission              = permissionsResult.HasViewAnyUserPermission,
                HasViewAnyLawyerAccountUserPermission = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                
                HasViewPublicUserPermission              = permissionsResult.HasViewPublicUserPermission,
                HasViewPublicLawyerAccountUserPermission = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                
                // [ACL]
                
                ViewUserPermissionId              = permission.ViewUserPermissionId,
                ViewLawyerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                 
                AttributeId  = parameters.AttributeId,
                UserId       = parameters.UserId,
                RoleId       = parameters.RoleId,

                Limit  = parameters.Pagination.End - parameters.Pagination.Begin + 1,
                Offset = parameters.Pagination.Begin - 1,

                NameFilter = string.IsNullOrWhiteSpace(parameters.Query) ? null : $"%{parameters.Query}%"
            };

            var queryText = $@"
SELECT
    [U].[id] AS [UserId],
    [L].[id] AS [LawyerId],
    [U].[name]
FROM [users] [U]
RIGHT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
WHERE
    (@NameFilter IS NULL OR [U].[name] LIKE @NameFilter)
    AND (

        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_USER | VIEW_USER]

        ([U].[id] = @UserId AND @HasViewOwnUserPermission = 1)

        OR

        (@ViewUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            LEFT JOIN [attributes] [A_PGR] ON [A_PGR].[id] = [PGR].[attribute_id]
            WHERE
                [PGR].[related_user_id] = [U].[id]            AND
                [PGR].[user_id]       = @UserId               AND
                [PGR].[permission_id] = @ViewUserPermissionId AND
                [PGR].[role_id]       = @RoleId               AND
                ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
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

    AND (
        
        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_LAWYER_ACCOUNT_USER | VIEW_LAWYER_ACCOUNT_USER]

        ([L].[user_id] = @UserId AND @HasViewOwnLawyerAccountUserPermission = 1)

        OR

        (@ViewLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            LEFT JOIN [attributes] [A_PGR] ON [A_PGR].[id] = [PGR].[attribute_id]
            WHERE
                [PGR].[related_user_id] = [U].[id]                             AND
                [PGR].[user_id]         = @UserId                              AND
                [PGR].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
                [PGR].[role_id]         = @RoleId                              AND
                ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
        ))

        OR

        @HasViewAnyLawyerAccountUserPermission = 1

        OR

        -- [Block 2: Lawyer Account is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicLawyerAccountUserPermission = 1
            OR @HasViewAnyLawyerAccountUserPermission = 1
        ))

    )

ORDER BY [L].[id] DESC
LIMIT @Limit OFFSET @Offset;";

            SearchInformation information;

            using (var multiple = await connection.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds
                    )))
            {
                information = new SearchInformation
                {
                    Items = await multiple.ReadAsync<SearchInformation.ItemProperties>()
                };
            }

            return information;
        });

        return resultConstructor.Build<SearchInformation>(information);
    }

    #endregion

    #region CountAsync

    public async Task<Result<CountInformation>> CountAsync(CountParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<CountInformation>();
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
            return resultConstructor.Build<CountInformation>().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build<CountInformation>().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<CountInformation>().Incorporate(roleIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build<CountInformation>().Incorporate(attributeAccountResult);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]
            
            ViewUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewOwnUserPermission',                 @ViewOwnUserPermissionId),
    ('HasViewOwnLawyerAccountUserPermission',    @ViewOwnLawyerAccountUserPermissionId),
    ('HasViewPublicUserPermission',              @ViewPublicUserPermissionId),
    ('HasViewPublicLawyerAccountUserPermission', @ViewPublicLawyerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                 @ViewAnyUserPermissionId),
    ('HasViewAnyLawyerAccountUserPermission',    @ViewAnyLawyerAccountUserPermissionId)
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
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                 AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                 AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyLawyerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId              = permission.ViewOwnUserPermissionId,               
                ViewOwnLawyerAccountUserPermissionId = permission.ViewOwnLawyerAccountUserPermissionId,

                ViewPublicUserPermissionId              = permission.ViewPublicUserPermissionId,               
                ViewPublicLawyerAccountUserPermissionId = permission.ViewPublicLawyerAccountUserPermissionId,

                ViewAnyUserPermissionId              = permission.ViewAnyUserPermissionId,
                ViewAnyLawyerAccountUserPermissionId = permission.ViewAnyLawyerAccountUserPermissionId,

                AttributeId = parameters.UserId,
                UserId      = parameters.UserId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Search>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                 // [NOT ACL]

                 HasViewOwnUserPermission              = permissionsResult.HasViewOwnUserPermission,
                 HasViewOwnLawyerAccountUserPermission = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                 
                 HasViewAnyUserPermission              = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyLawyerAccountUserPermission = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                 
                 HasViewPublicUserPermission              = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicLawyerAccountUserPermission = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewLawyerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 AttributeId  = parameters.AttributeId,
                 UserId       = parameters.UserId,
                 RoleId       = parameters.RoleId,

                 NameFilter = string.IsNullOrWhiteSpace(parameters.Query) ? null : $"%{_hashService.Encrypt(parameters.Query)}%"
            };

            var queryText = $@"
SELECT
    COUNT(*)
FROM [users] [U]
RIGHT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
WHERE
    (@NameFilter IS NULL OR [U].[name] LIKE @NameFilter)
    AND (

        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_USER | VIEW_USER]

        ([U].[id] = @UserId AND @HasViewOwnUserPermission = 1)

        OR

        (@ViewUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            LEFT JOIN [attributes] [A_PGR] ON [A_PGR].[id] = [PGR].[attribute_id]
            WHERE
                [PGR].[related_user_id] = [U].[id]              AND
                [PGR].[user_id]         = @UserId               AND
                [PGR].[permission_id]   = @ViewUserPermissionId AND
                [PGR].[role_id]         = @RoleId               AND
                ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
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

    AND (
        
        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_LAWYER_ACCOUNT_USER | VIEW_LAWYER_ACCOUNT_USER]

        ([L].[user_id] = @UserId AND @HasViewOwnLawyerAccountUserPermission = 1)

        OR

        (@ViewLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            LEFT JOIN [attributes] [A_PGR] ON [A_PGR].[id] = [PGR].[attribute_id]
            WHERE
                [PGR].[related_user_id] = [U].[id]                           AND
                [PGR].[user_id]         = @UserId                            AND
                [PGR].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
                [PGR].[role_id]         = @RoleId                            AND
                ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
        ))

        OR

        @HasViewAnyLawyerAccountUserPermission = 1

        OR

        -- [Block 2: Lawyer Account is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicLawyerAccountUserPermission = 1
            OR @HasViewAnyLawyerAccountUserPermission = 1
        ))

    );";

            return new CountInformation()
            {
                Count = await connection.Connection.QueryFirstAsync<long>(new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds
                    ))
            };
        });

        return resultConstructor.Build<CountInformation>(information);
    }

    #endregion

    #region DetailsAsync

    public async Task<Result<DetailsInformation>> DetailsAsync(DetailsParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(new NotFoundDatabaseConnectionStringError());

            return resultConstructor.Build<DetailsInformation>();
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
            return resultConstructor.Build<DetailsInformation>().Incorporate(userIdResult);

        // [Attribute Id]
        var attributeIdResult = await ValidateAttributeId(
            parameters.AttributeId,
            contextualizer);

        if (attributeIdResult.IsFinished)
            return resultConstructor.Build<DetailsInformation>().Incorporate(attributeIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<DetailsInformation>().Incorporate(roleIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build<DetailsInformation>().Incorporate(attributeAccountResult);

        var permission = new
        {
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]
            
            ViewUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer)
        };

        // [Permissions Queries]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewUserPermission',                    @ViewUserPermissionId),
    ('HasViewLawyerAccountUserPermission',       @ViewLawyerAccountUserPermissionId),
    ('HasViewOwnUserPermission',                 @ViewOwnUserPermissionId),
    ('HasViewOwnLawyerAccountUserPermission',    @ViewOwnLawyerAccountUserPermissionId),
    ('HasViewPublicUserPermission',              @ViewPublicUserPermissionId),
    ('HasViewPublicLawyerAccountUserPermission', @ViewPublicLawyerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                 @ViewAnyUserPermissionId),
    ('HasViewAnyLawyerAccountUserPermission',    @ViewAnyLawyerAccountUserPermissionId)
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

    -- [ACL grants]
        SELECT
        [PC].[permission_name],
        [PGR].[attribute_id],
        1 AS [granted]
    FROM [permission_checks] [PC]
    JOIN [permission_grants_relationship] [PGR]
      ON [PGR].[permission_id]     = [PC].[permission_id] AND
         [PGR].[user_id]           = @UserId              AND
         [PGR].[role_id]           = @RoleId              AND
         [PGR].[related_user_id]   = (SELECT [U].[id] FROM [Users] [U] LEFT JOIN [lawyers] [L] ON [U].[id] = [L].[user_id] WHERE [L].[id] = @LawyerId)
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUserPermission'                    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewLawyerAccountUserPermission'       AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                 AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnLawyerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicLawyerAccountUserPermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicLawyerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                 AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyLawyerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyLawyerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new 
        { 
            ViewUserPermissionId              = permission.ViewUserPermissionId,
            ViewLawyerAccountUserPermissionId = permission.ViewLawyerAccountUserPermissionId,
        
            ViewOwnUserPermissionId              = permission.ViewOwnUserPermissionId,               
            ViewOwnLawyerAccountUserPermissionId = permission.ViewOwnLawyerAccountUserPermissionId,
        
            ViewPublicUserPermissionId              = permission.ViewPublicUserPermissionId,               
            ViewPublicLawyerAccountUserPermissionId = permission.ViewPublicLawyerAccountUserPermissionId,
        
            ViewAnyUserPermissionId              = permission.ViewAnyUserPermissionId,
            ViewAnyLawyerAccountUserPermissionId = permission.ViewAnyLawyerAccountUserPermissionId,
        
            AttributeId = parameters.AttributeId,
            UserId      = parameters.UserId,
            LawyerId    = parameters.LawyerId,
            RoleId      = parameters.RoleId,
        };
        
        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Details>(queryPermissions, queryPermissionsParameters);
        
        // [Principal Query]
        
        var queryParameters = new
        {
             // [NOT ACL]
        
             HasViewOwnUserPermission              = permissionsResult.HasViewOwnUserPermission,
             HasViewOwnLawyerAccountUserPermission = permissionsResult.HasViewOwnLawyerAccountUserPermission,
             
             HasViewAnyUserPermission              = permissionsResult.HasViewAnyUserPermission,
             HasViewAnyLawyerAccountUserPermission = permissionsResult.HasViewAnyLawyerAccountUserPermission,
             
             HasViewPublicUserPermission              = permissionsResult.HasViewPublicUserPermission,
             HasViewPublicLawyerAccountUserPermission = permissionsResult.HasViewPublicLawyerAccountUserPermission,
             
             // [ACL]
             
             HasViewUserPermission              = permissionsResult.HasViewUserPermission,
             HasViewLawyerAccountUserPermission = permissionsResult.HasViewLawyerAccountUserPermission,
                  
             LawyerId     = parameters.LawyerId,
             AttributeId  = parameters.AttributeId,
             UserId       = parameters.UserId,
             RoleId       = parameters.RoleId
        };
        
        var queryText = $@"
SELECT
    [U].[id] AS [UserId],
    [L].[id] AS [LawyerId],
    [U].[name]
FROM [users] [U]
RIGHT JOIN [lawyers] [L] ON [L].[user_id] = [U].[id]
WHERE
    [L].[id] = @LawyerId
    AND (

        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_USER | VIEW_USER]

        ([U].[id] = @UserId AND (
            @HasViewOwnUserPermission    = 1 
            OR @HasViewAnyUserPermission = 1)

        OR

        (@HasViewUserPermission = 1 OR @HasViewAnyUserPermission = 1)

        OR

        @HasViewAnyUserPermission = 1

        OR

        -- [Block 2: User is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicUserPermission = 1
            OR @HasViewAnyUserPermission = 1
        ))
    )

    AND (
        
        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_LAWYER_ACCOUNT_USER | VIEW_LAWYER_ACCOUNT_USER]

        ([L].[user_id] = @UserId AND @HasViewOwnLawyerAccountUserPermission = 1)

        OR

        (@HasViewLawyerAccountUserPermission = 1 OR @HasViewAnyLawyerAccountUserPermission = 1)

        OR

        @HasViewAnyLawyerAccountUserPermission = 1

        OR

        -- [Block 2: Lawyer Account is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicLawyerAccountUserPermission = 1
            OR @HasViewAnyLawyerAccountUserPermission = 1
        ))
));";

        var item = await connection.Connection.QueryFirstOrDefaultAsync<DetailsInformation.ItemProperties>(
            new CommandDefinition(
                commandText:       queryText,
                parameters:        queryParameters,
                transaction:       connection.Transaction,
                cancellationToken: contextualizer.CancellationToken,
                commandTimeout:   TimeSpan.FromHours(1).Milliseconds
                ));

        if (item == null)
        {
            resultConstructor.SetConstructor(new LawyerNotFoundError());

            return resultConstructor.Build<DetailsInformation>();
        }

        var information = new DetailsInformation()
        {
            Item = item
        };

        return resultConstructor.Build<DetailsInformation>(information);
    }

    #endregion

    #region RegisterAsync

    public async Task<Result> RegisterAsync(RegisterParameters parameters, Contextualizer contextualizer)
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
            // [Related to USER or ROLE permission]

            RegisterLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REGISTER_LAWYER_ACCOUNT_USER, contextualizer)
        };

        // [User Id]
        var userIdResult = await ValidateUserId(
            parameters.RoleId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(userIdResult);

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(roleIdResult);

        // [Permission Validation]

        // [Permission Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasRegisterLawyerAccountUserPermission', @RegisterLawyerAccountUserPermissionId)
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
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasRegisterLawyerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRegisterLawyerAccountUserPermission]  
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            RegisterLawyerAccountUserPermissionId = permission.RegisterLawyerAccountUserPermissionId,

            UserId = parameters.UserId,
            RoleId = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Register>(queryPermissions, queryPermissionsParameters);


        if (!permissionsResult.HasRegisterLawyerAccountUserPermission)
        {
            resultConstructor.SetConstructor(new RegisterDeniedError());
    
            return resultConstructor.Build();
        }

        var userAlreadyHaveLawyerAccount = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedPhone = _hashService.Encrypt(parameters.Phone);

            var queryParameters = new
            {
                UserId  = parameters.UserId,
                Phone   = encrpytedPhone
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT CASE WHEN EXISTS (SELECT 1 FROM [lawyers] L WHERE [L].[user_id] = @UserId) 
                                        THEN 1 ELSE 0 
                                   END AS [user_already_have_lawyers_account]");

            var userAlreadyHaveLawyerAccount = await connection.Connection.QueryFirstAsync<bool>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return userAlreadyHaveLawyerAccount;
        });

        if (userAlreadyHaveLawyerAccount)
        {
            resultConstructor.SetConstructor(
                new UserAlreadyHaveLawyerAccountError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedPhone   = _hashService.Encrypt(parameters.Phone);

            var queryParameters = new 
            {
                UserId  = parameters.UserId,
                Phone   = encrpytedPhone
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("INSERT INTO [lawyers] ([user_id], [phone]) VALUES (@UserId, @Phone)");

            var includedItems = await connection.Connection.ExecuteAsync(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return includedItems;
        });

        if (includedItems == 0)
        {
            resultConstructor.SetConstructor(new RegisterInsertionError());

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }

    #endregion

    #region Validations

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