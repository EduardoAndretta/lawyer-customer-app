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
SELECT

/* ---------------------------------------------- [VIEW_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnUserPermission],

/* ---------------------------------------------- [VIEW_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyUserPermission],

/* ---------------------------------------------- [VIEW_ANY_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                     AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                     AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission]";

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

                 HasViewOwnUserPermission                = permissionsResult.HasViewOwnUserPermission,
                 HasViewOwnLawyerAccountUserPermission = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                 
                 HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyLawyerAccountUserPermission = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                 
                 HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicLawyerAccountUserPermission = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewLawyerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
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
SELECT

/* ---------------------------------------------- [VIEW_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnUserPermission],

/* ---------------------------------------------- [VIEW_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyUserPermission],

/* ---------------------------------------------- [VIEW_ANY_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                     AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                     AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission]";

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

                 HasViewOwnUserPermission                = permissionsResult.HasViewOwnUserPermission,
                 HasViewOwnLawyerAccountUserPermission = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                 
                 HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyLawyerAccountUserPermission = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                 
                 HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicLawyerAccountUserPermission = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewLawyerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 AttributeId  = parameters.AttributeId,
                 UserId       = parameters.UserId,
                 RoleId       = parameters.RoleId
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
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            DetailsInformation information;

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
SELECT

/* ---------------------------------------------- [VIEW_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnUserPermission],

/* ---------------------------------------------- [VIEW_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyUserPermission],

/* ---------------------------------------------- [VIEW_ANY_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                     AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                     AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission]";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,               
                ViewOwnLawyerAccountUserPermissionId = permission.ViewOwnLawyerAccountUserPermissionId,

                ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
                ViewPublicLawyerAccountUserPermissionId = permission.ViewPublicLawyerAccountUserPermissionId,

                ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
                ViewAnyLawyerAccountUserPermissionId = permission.ViewAnyLawyerAccountUserPermissionId,

                AttributeId = parameters.UserId,
                UserId      = parameters.UserId,
                LawyerId    = parameters.LawyerId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Details>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                 // [NOT ACL]

                 HasViewOwnUserPermission                = permissionsResult.HasViewOwnUserPermission,
                 HasViewOwnLawyerAccountUserPermission = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                 
                 HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyLawyerAccountUserPermission = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                 
                 HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicLawyerAccountUserPermission = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewLawyerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 AttributeId  = parameters.AttributeId,
                 UserId       = parameters.UserId,
                 RoleId       = parameters.RoleId
            };

            var queryText = $@"
SELECT
    [U].[id] AS [UserId],
    [L].[id] AS [LawyerId],
    [U].[name],
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

WHERE [U].[id] = @UserId AND [L].[id] = @LawyerId;";

            using (var multiple = await connection.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds
                    )))
            {
                information = new DetailsInformation
                {
                    Item = await multiple.ReadFirstAsync<DetailsInformation.ItemProperties>()
                };
            }

            return information;
        });

        return resultConstructor.Build<DetailsInformation>(information);
    }

    public async Task<Result> RegisterAsync(RegisterParameters parameters, Contextualizer contextualizer)
    {
        var resultConstructor = new ResultConstructor();

        var sqliteConnectionString = _configuration.GetConnectionString("Sqlite");

        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            resultConstructor.SetConstructor(
                new NotFoundDatabaseConnectionStringError()
                {
                    Status = 500
                });

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
SELECT

/* ---------------------------------------------- [REGISTER_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants (Role Grant)] [REGISTER_LAWYER_ACCOUNT_USER]

    (@RegisterLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @RegisterLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRegisterLawyerAccountUserPermission]";

        var queryPermissionsParameters = new
        {
            RegisterLawyerAccountUserPermissionId = permission.RegisterLawyerAccountUserPermissionId,

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
            var encrpytedAddress = _hashService.Encrypt(parameters.Address);
            var encrpytedPhone   = _hashService.Encrypt(parameters.Phone);

            var queryParameters = new
            {
                UserId  = parameters.UserId,
                Address = encrpytedAddress,
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
            var encrpytedAddress = _hashService.Encrypt(parameters.Address);
            var encrpytedPhone   = _hashService.Encrypt(parameters.Phone);

            var queryParameters = new 
            {
                UserId  = parameters.UserId,
                Address = encrpytedAddress,
                Phone   = encrpytedPhone
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("INSERT INTO [lawyers] ([user_id], [address], [phone]) VALUES (@UserId, @Address, @Phone)");

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
                        WHEN UPPER([A].[name]) = 'LAWYER' THEN 
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