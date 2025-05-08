using Dapper;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.Domain.User.Interfaces.Services;
using LawyerCustomerApp.Domain.User.Repositories.Models;
using LawyerCustomerApp.Domain.User.Responses.Repositories.Error;
using LawyerCustomerApp.Domain.User.Responses.Repositories.Success;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Text;

using PermissionSymbols = LawyerCustomerApp.External.Models.Permission.Permissions;

namespace LawyerCustomerApp.Domain.User.Repositories;

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
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
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

/* ---------------------------------------------- [VIEW_OWN_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCustomerAccountUserPermission],

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
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCustomerAccountUserPermission],

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
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                  AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                  AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCustomerAccountUserPermission]";

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
                 HasViewOwnLawyerAccountUserPermission   = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                 HasViewOwnCustomerAccountUserPermission = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                 
                 HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyLawyerAccountUserPermission   = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                 HasViewAnyCustomerAccountUserPermission = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                 
                 HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicLawyerAccountUserPermission   = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                 HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                 ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 AttributeId  = parameters.AttributeId,
                 UserId       = parameters.UserId,
                 RoleId       = parameters.RoleId
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
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                            LEFT JOIN [attributes] [Al] ON [Al].[id] = [PGRl].[attribute_id]
                            WHERE
                                [PGRl].[related_user_id] = @UserId                            AND 
                                [PGRl].[user_id]         = @ExternalUserId                    AND 
                                [PGRl].[role_id]         = @RoleId                            AND 
                                [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND 
                                ([PGRl].[attribute_id] IS NULL OR [Al].[id] = @AttributeId)
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
        THEN [L].[id]
        ELSE NULL
    END AS [LawyerId],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
                    )
                    OR @HasViewAnyUserPermission = 1
                ))
                OR ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [C].[private] = 1 AND (
                        @ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1
                            FROM [permission_grants_relationship] [PGRc]
                            LEFT JOIN [attributes] [Ac] ON [Ac].[id] = [PGRc].[attribute_id]
                            WHERE
                                [PGRc].[related_user_id] = @UserId                              AND 
                                [PGRc].[user_id]         = @ExternalUserId                      AND 
                                [PGRc].[role_id]         = @RoleId                              AND 
                                [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                ([PGRc].[attribute_id] IS NULL OR [Ac].[id] = @AttributeId)
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
        THEN [C].[id]
        ELSE NULL
    END AS [CustomerId],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId                AND 
                            [PGRu].[user_id]         = @ExternalUserId        AND 
                            [PGRu].[role_id]         = @RoleId                AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId  AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                            LEFT JOIN [attributes] [Al] ON [Al].[id] = [PGRl].[attribute_id]
                            WHERE
                                [PGRl].[related_user_id] = @UserId                            AND 
                                [PGRl].[user_id]         = @ExternalUserId                    AND 
                                [PGRl].[role_id]         = @RoleId                            AND 
                                [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND 
                                ([PGRl].[attribute_id] IS NULL OR [Al].[id] = @AttributeId)
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
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                            LEFT JOIN [attributes] [Ac] ON [Ac].[id] = [PGRc].[attribute_id]
                            WHERE
                                [PGRc].[related_user_id] = @UserId                              AND 
                                [PGRc].[user_id]         = @ExternalUserId                      AND 
                                [PGRc].[role_id]         = @RoleId                              AND 
                                [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                ([PGRc].[attribute_id] IS NULL OR [Ac].[id] = @AttributeId)
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
    END AS [HasCustomerAccount]

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

ORDER BY [U].[id] DESC
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
            
            ViewUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewOwnUserPermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer)
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
END AS [HasViewOwnUserPermission]

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
END AS [HasViewAnyUserPermission]

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
END AS [HasViewPublicUserPermission]";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId    = permission.ViewOwnUserPermissionId,               
                ViewPublicUserPermissionId = permission.ViewPublicUserPermissionId,               
                ViewAnyUserPermissionId    = permission.ViewAnyUserPermissionId,

                AttributeId = parameters.UserId,
                UserId      = parameters.UserId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Search>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                 // [NOT ACL]

                 HasViewOwnUserPermission    = permissionsResult.HasViewOwnUserPermission,
                 HasViewAnyUserPermission    = permissionsResult.HasViewAnyUserPermission,
                 HasViewPublicUserPermission = permissionsResult.HasViewPublicUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 AttributeId  = parameters.AttributeId,
                 UserId       = parameters.UserId,
                 RoleId       = parameters.RoleId
            };

            var queryText = $@"
SELECT 
    COUNT(*)
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
    )";

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
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
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

/* ---------------------------------------------- [VIEW_OWN_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCustomerAccountUserPermission],

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
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCustomerAccountUserPermission],

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
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                  AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                  AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCustomerAccountUserPermission]";

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
                 HasViewOwnLawyerAccountUserPermission   = permissionsResult.HasViewOwnLawyerAccountUserPermission,
                 HasViewOwnCustomerAccountUserPermission = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                 
                 HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyLawyerAccountUserPermission   = permissionsResult.HasViewAnyLawyerAccountUserPermission,
                 HasViewAnyCustomerAccountUserPermission = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                 
                 HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicLawyerAccountUserPermission   = permissionsResult.HasViewPublicLawyerAccountUserPermission,
                 HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                 ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 AttributeId   = parameters.AttributeId,
                 UserId        = parameters.UserId,
                 RelatedUserId = parameters.RelatedUserId,
                 RoleId        = parameters.RoleId
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
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                            LEFT JOIN [attributes] [Al] ON [Al].[id] = [PGRl].[attribute_id]
                            WHERE
                                [PGRl].[related_user_id] = @UserId                            AND 
                                [PGRl].[user_id]         = @ExternalUserId                    AND 
                                [PGRl].[role_id]         = @RoleId                            AND 
                                [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND 
                                ([PGRl].[attribute_id] IS NULL OR [Al].[id] = @AttributeId)
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
        THEN [L].[id]
        ELSE NULL
    END AS [LawyerId],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
                    )
                    OR @HasViewAnyUserPermission = 1
                ))
                OR ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [C].[private] = 1 AND (
                        @ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
                            SELECT 1
                            FROM [permission_grants_relationship] [PGRc]
                            LEFT JOIN [attributes] [Ac] ON [Ac].[id] = [PGRc].[attribute_id]
                            WHERE
                                [PGRc].[related_user_id] = @UserId                              AND 
                                [PGRc].[user_id]         = @ExternalUserId                      AND 
                                [PGRc].[role_id]         = @RoleId                              AND 
                                [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                ([PGRc].[attribute_id] IS NULL OR [Ac].[id] = @AttributeId)
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
        THEN [C].[id]
        ELSE NULL
    END AS [CustomerId],

    CASE
        WHEN
            (
                ([U].[private] = 1 AND (
                    @ViewUserPermissionId IS NOT NULL AND EXISTS (
                        SELECT 1
                        FROM [permission_grants_relationship] [PGRu]
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId                AND 
                            [PGRu].[user_id]         = @ExternalUserId        AND 
                            [PGRu].[role_id]         = @RoleId                AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId  AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                            LEFT JOIN [attributes] [Al] ON [Al].[id] = [PGRl].[attribute_id]
                            WHERE
                                [PGRl].[related_user_id] = @UserId                            AND 
                                [PGRl].[user_id]         = @ExternalUserId                    AND 
                                [PGRl].[role_id]         = @RoleId                            AND 
                                [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND 
                                ([PGRl].[attribute_id] IS NULL OR [Al].[id] = @AttributeId)
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
                        LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                        WHERE
                            [PGRu].[related_user_id] = @UserId               AND 
                            [PGRu].[user_id]         = @ExternalUserId       AND 
                            [PGRu].[role_id]         = @RoleId               AND 
                            [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                            ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                            LEFT JOIN [attributes] [Ac] ON [Ac].[id] = [PGRc].[attribute_id]
                            WHERE
                                [PGRc].[related_user_id] = @UserId                              AND 
                                [PGRc].[user_id]         = @ExternalUserId                      AND 
                                [PGRc].[role_id]         = @RoleId                              AND 
                                [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                ([PGRc].[attribute_id] IS NULL OR [Ac].[id] = @AttributeId)
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
    END AS [HasCustomerAccount]

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

WHERE [U].[id] = @RelatedUserId;";

            DetailsInformation information;

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

            RegisterUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REGISTER_USER, contextualizer)
        };

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(roleIdResult);

        // [Permission Validation]

        const string queryPermissions = @"
SELECT

/* ---------------------------------------------- [REGISTER_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants (Role Grant)] [REGISTER_USER]

    (@RegisterUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @RegisterUserPermissionId AND
            [PG].[role_id]       = @RoleId                   AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRegisterUserPermission]";

        var queryPermissionsParameters = new
        {
            RegisterUserPermissionId = permission.RegisterUserPermissionId,

            RoleId = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Register>(queryPermissions, queryPermissionsParameters);

        if (!permissionsResult.HasRegisterUserPermission)
        {
            resultConstructor.SetConstructor(new RegisterUserDeniedError());

            return resultConstructor.Build();
        }

        var emailAlreadyInUse = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedEmail    = _hashService.Encrypt(parameters.Email);
            var encrpytedPassword = _hashService.Encrypt(parameters.Password);

            var queryParameters = new
            {
                Email = encrpytedEmail
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT CASE WHEN EXISTS (SELECT 1 FROM [users] U WHERE U.[email] = @Email) 
                                        THEN 1 ELSE 0 
                                   END AS [email_already_in_use]");

            var emailAlreadyInUse = await connection.Connection.QueryFirstAsync<bool>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return emailAlreadyInUse;
        });

        if (emailAlreadyInUse)
        {
            resultConstructor.SetConstructor(new EmailAlreadyInUseError());

            return resultConstructor.Build();
        }

        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedEmail    = _hashService.Encrypt(parameters.Email);
            var encrpytedPassword = _hashService.Encrypt(parameters.Password);
            var encrpytedName     = _hashService.Encrypt(parameters.Name);

            var queryParameters = new 
            {
                Email        = encrpytedEmail,
                Password     = encrpytedPassword,
                Name         = encrpytedName,
                RegisterDate = DateTime.Now
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("INSERT INTO [users] ([email], [password], [name], [register_date]) VALUES (@Email, @Password, @Name, @RegisterDate)");

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
            resultConstructor.SetConstructor(new UserInsertionError());

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }

    public async Task<Result> EditAsync(EditParameters parameters, Contextualizer contextualizer)
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

            EditUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.EDIT_USER, contextualizer),
            EditLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.EDIT_LAWYER_ACCOUNT_USER, contextualizer),
            EditCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            EditOwnUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_OWN_USER, contextualizer),

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            EditAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_USER, contextualizer),
            EditAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            EditAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer), 
        };

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(roleIdResult);

        // [uSER Id]
        var userIdResult = await ValidateUserId(
            parameters.UserId,
            contextualizer);

        if (userIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(userIdResult);

        // [Permission Validation]

        const string queryPermissions = @"
SELECT

/* ---------------------------------------------- [EDIT_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [EDIT_OWN_USER]

    (@EditOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @EditOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [EDIT_OWN_USER]

    (@EditOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @EditOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasEditOwnUserPermission],

/* ---------------------------------------------- [EDIT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [EDIT_USER]

    (@EditUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId        AND
            [PGR].[user_id]         = @UserId               AND
            [PGR].[permission_id]   = @EditUserPermissionId AND
            [PGR].[role_id]         = @RoleId               AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasEditUserPermission],

/* ---------------------------------------------- [EDIT_ANY_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [EDIT_ANY_USER]

    (@EditAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @EditAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [EDIT_ANY_USER]

    (@EditAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @EditAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasEditAnyUserPermission],

/* ---------------------------------------------- [EDIT_ANY_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [EDIT_ANY_LAWYER_ACCOUNT_USER]

    (@EditAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @EditAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [EDIT_ANY_LAWYER_ACCOUNT_USER]

    (@EditAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @EditAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasEditAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [EDIT_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [EDIT_ANY_CUSTOMER_ACCOUNT_USER]

    (@EditAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @EditAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [EDIT_ANY_CUSTOMER_ACCOUNT_USER]

    (@EditAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @EditAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasEditAnyCustomerAccountUserPermission],

/* ---------------------------------------------- [EDIT_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [EDIT_LAWYER_ACCOUNT_USER]

    (@EditLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                     AND
            [PGR].[user_id]         = @UserId                            AND
            [PGR].[permission_id]   = @EditLawyerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId                            AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasEditLawyerAccountUserPermission],

/* ---------------------------------------------- [EDIT_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [EDIT_CUSTOMER_ACCOUNT_USER]

    (@EditCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                       AND
            [PGR].[user_id]         = @UserId                              AND
            [PGR].[permission_id]   = @EditCustomerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId                              AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasEditCustomerAccountUserPermission],

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

/* ---------------------------------------------- [VIEW_OWN_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCustomerAccountUserPermission],

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
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCustomerAccountUserPermission],

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
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                  AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                  AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCustomerAccountUserPermission],

/* ---------------------------------------------- [VIEW_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [VIEW_USER]

    (@ViewUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId        AND
            [PGR].[user_id]         = @UserId               AND
            [PGR].[permission_id]   = @ViewUserPermissionId AND
            [PGR].[role_id]         = @RoleId               AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewUserPermission],

/* ---------------------------------------------- [VIEW_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [VIEW_LAWYER_ACCOUNT_USER]

    (@ViewLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                     AND
            [PGR].[user_id]         = @UserId                            AND
            [PGR].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId                            AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [VIEW_CUSTOMER_ACCOUNT_USER]

    (@ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                       AND
            [PGR].[user_id]         = @UserId                              AND
            [PGR].[permission_id]   = @ViewCustomerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId                              AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewCustomerAccountUserPermission]";

        var queryPermissionsParameters = new
        {
            EditUserPermissionId                = permission.EditUserPermissionId,
            EditLawyerAccountUserPermissionId   = permission.EditLawyerAccountUserPermissionId,
            EditCustomerAccountUserPermissionId = permission.EditCustomerAccountUserPermissionId,

            EditOwnUserPermissionId = permission.EditOwnUserPermissionId,

            EditAnyUserPermissionId                = permission.EditAnyUserPermissionId,
            EditAnyLawyerAccountUserPermissionId   = permission.EditAnyLawyerAccountUserPermissionId,
            EditAnyCustomerAccountUserPermissionId = permission.EditAnyCustomerAccountUserPermissionId,

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

            RelatedUserId = parameters.RelatedUserId,
            UserId        = parameters.UserId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Edit>(queryPermissions, queryPermissionsParameters);

        // [User Information]

        const string queryUserInformations = @"
SELECT 

[U].[private] AS [Private], 

CASE WHEN [U].[id] = @UserId THEN 1, ELSE 0 END AS [Owner],

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [lawyers] [L] WHERE [L].[user_id] = @RelatedUserId))
    THEN 1
    ELSE 0
END AS [HasLawyerAccount],

IFNULL(SELECT [L].[id] FROM [lawyers] [L] WHERE [L].[user_id] = @RelatedUserId, 0) AS [LawyerId],

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [customers] [C] WHERE [C].[user_id] = @RelatedUserId))
    THEN 1
    ELSE 0
END AS [HasCustomerAccount],

IFNULL(SELECT [C].[id] FROM [customers] [C] WHERE [C].[user_id] = @RelatedUserId, 0) AS [CustomerId]

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [address_users] [AU] WHERE [AU].[user_id] = @RelatedUserId))
    THEN 1
    ELSE 0
END AS [HasAddress],

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [documents_users] [DU] WHERE [DU].[user_id] = @RelatedUserId))
    THEN 1
    ELSE 0
END AS [HasDocument],

FROM [users] [U] WHERE [U].[id] = @RelatedUserId";

        var queryUserInformationParameters = new
        {
            RelatedUserId = parameters.RelatedUserId,
            UserId        = parameters.UserId
        };

        var userInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner, bool? HasLawyerAccount, int? LawyerId, bool? HasCustomerAccount, int? CustomerId, bool? HasAddress, bool? HasDocument)>(queryUserInformations, queryUserInformationParameters);

        // [VIEW]
        if (((userInformationResult.Private.HasValue && userInformationResult.Private.Value) && !permissionsResult.HasViewPublicUserPermission) &&
            ((userInformationResult.Owner.HasValue   && userInformationResult.Owner.Value)   && !permissionsResult.HasViewOwnUserPermission)    &&
            !permissionsResult.HasViewUserPermission &&
            !permissionsResult.HasViewAnyUserPermission)
        {
            resultConstructor.SetConstructor(new UserNotFoundError());

            return resultConstructor.Build();
        }

        // [EDIT]
        if (((userInformationResult.Owner.HasValue && userInformationResult.Owner.Value) && !permissionsResult.HasEditOwnUserPermission) &&
            !permissionsResult.HasEditUserPermission &&
            !permissionsResult.HasEditAnyUserPermission)
        {
            resultConstructor.SetConstructor(new EditDeniedError());

            return resultConstructor.Build();
        }

        await _databaseService.Execute(
            connection, 
            async () =>
            {
                var queryParameters = new DynamicParameters();

                var dynamicSetStattement = new Collection<string>();

                var dynamicInsertStattement = new Collection<string>();
                var dynamicValuesStattement = new Collection<string>();

                // =================== [Table - users] =================== //

                if (parameters.HasChanges)
                {
                    queryParameters.Add("@UserId", parameters.RelatedUserId);

                    // [Private]
                    if (parameters.Private.Received)
                    {
                        dynamicSetStattement.Add("SET [private] = @Private");
                        queryParameters.Add("@Private", parameters.Private.Value);
                    }

                    if (!dynamicSetStattement.Any())
                    {
                        var query = $"UPDATE [users] {string.Join("AND", dynamicSetStattement)} WHERE [id] = @UserId";

                        var includedItems = await connection.Connection.ExecuteAsync(
                            new CommandDefinition(
                                    commandText:       query,
                                    parameters:        queryParameters,
                                    transaction:       connection.Transaction,
                                    cancellationToken: contextualizer.CancellationToken,
                                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                    }
                }

                // =================== [Table - address_users] =================== //

                if (parameters.Address.HasChanges)
                {
                    queryParameters = new DynamicParameters();

                    dynamicInsertStattement.Clear();
                    dynamicValuesStattement.Clear();

                    dynamicSetStattement.Clear();

                    if (!userInformationResult.HasAddress.HasValue || !userInformationResult.HasAddress.Value)
                    {
                        dynamicInsertStattement.Add("[user_id]");
                        dynamicValuesStattement.Add("@UserId");

                        queryParameters.Add("@UserId", parameters.RelatedUserId);

                        // [ZipCode]
                        if (parameters.Address.ZipCode.Received)
                        {
                            dynamicInsertStattement.Add("[zip_code]");
                            dynamicValuesStattement.Add("@ZipCode");
                            queryParameters.Add("@ZipCode", parameters.Address.ZipCode.Value);
                        }

                        // [HouseNumber]
                        if (parameters.Address.HouseNumber.Received)
                        {
                            dynamicInsertStattement.Add("[house_number]");
                            dynamicValuesStattement.Add("@HouseNumber");
                            queryParameters.Add("@HouseNumber", parameters.Address.HouseNumber.Value);
                        }

                        // [Complement]
                        if (parameters.Address.Complement.Received)
                        {
                            dynamicInsertStattement.Add("[complement]");
                            dynamicValuesStattement.Add("@Complement");
                            queryParameters.Add("@Complement", parameters.Address.Complement.Value);
                        }

                        // [District]
                        if (parameters.Address.District.Received)
                        {
                            dynamicInsertStattement.Add("[district]");
                            dynamicValuesStattement.Add("@District");
                            queryParameters.Add("@District", parameters.Address.District.Value);
                        }

                        // [City]
                        if (parameters.Address.City.Received)
                        {
                            dynamicInsertStattement.Add("[city]");
                            dynamicValuesStattement.Add("@City");
                            queryParameters.Add("@City", parameters.Address.City.Value);
                        }

                        // [State]
                        if (parameters.Address.State.Received)
                        {
                            dynamicInsertStattement.Add("[state]");
                            dynamicValuesStattement.Add("@State");
                            queryParameters.Add("@State", parameters.Address.State.Value);
                        }

                        // [Country]
                        if (parameters.Address.Country.Received)
                        {
                            dynamicInsertStattement.Add("[country]");
                            dynamicValuesStattement.Add("@Country");
                            queryParameters.Add("@Country", parameters.Address.Country.Value);
                        }

                        if (!dynamicSetStattement.Any())
                        {
                            var query = $"INSERT INTO [address_users] ({string.Join(",", dynamicInsertStattement)}) VALUES ({string.Join(",", dynamicValuesStattement)})";

                            await connection.Connection.ExecuteAsync(
                                new CommandDefinition(
                                        commandText:       query,
                                        parameters:        queryParameters,
                                        transaction:       connection.Transaction,
                                        cancellationToken: contextualizer.CancellationToken,
                                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                        }
                    }
                    else
                    {  
                        // [ZipCode]
                        if (parameters.Address.ZipCode.Received)
                        {
                            dynamicSetStattement.Add("SET [zip_code] = @ZipCode");
                            queryParameters.Add("@ZipCode", parameters.Address.ZipCode.Value);
                        }

                        // [HouseNumber]
                        if (parameters.Address.HouseNumber.Received)
                        {
                            dynamicSetStattement.Add("SET [house_number] = @HouseNumber");
                            queryParameters.Add("@HouseNumber", parameters.Address.HouseNumber.Value);
                        }

                        // [Complement]
                        if (parameters.Address.Complement.Received)
                        {
                            dynamicSetStattement.Add("SET [complement] = @Complement");
                            queryParameters.Add("@Complement", parameters.Address.Complement.Value);
                        }

                        // [District]
                        if (parameters.Address.District.Received)
                        {
                            dynamicSetStattement.Add("SET [district] = @District");
                            queryParameters.Add("@District", parameters.Address.District.Value);
                        }

                        // [City]
                        if (parameters.Address.City.Received)
                        {
                            dynamicSetStattement.Add("SET [city] = @City");
                            queryParameters.Add("@City", parameters.Address.City.Value);
                        }

                        // [State]
                        if (parameters.Address.State.Received)
                        {
                            dynamicSetStattement.Add("SET [state] = @State");
                            queryParameters.Add("@State", parameters.Address.State.Value);
                        }

                        // [Country]
                        if (parameters.Address.Country.Received)
                        {
                            dynamicSetStattement.Add("SET [country] = @Country");
                            queryParameters.Add("@Country", parameters.Address.Country.Value);
                        }

                        if (!dynamicSetStattement.Any())
                        {
                            var query = $"INSERT INTO [documents_users] ({string.Join(",", dynamicInsertStattement)}) VALUES ({string.Join(",", dynamicValuesStattement)})";

                            await connection.Connection.ExecuteAsync(
                                new CommandDefinition(
                                        commandText:       query,
                                        parameters:        queryParameters,
                                        transaction:       connection.Transaction,
                                        cancellationToken: contextualizer.CancellationToken,
                                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                        }
                    }
                }

                // =================== [Table - documents_users] =================== //

                if (parameters.Document.HasChanges)
                {
                    queryParameters = new DynamicParameters();

                    dynamicInsertStattement.Clear();
                    dynamicValuesStattement.Clear();

                    dynamicSetStattement.Clear();

                    if (!userInformationResult.HasAddress.HasValue || !userInformationResult.HasAddress.Value)
                    {
                        dynamicInsertStattement.Add("[user_id]");
                        dynamicValuesStattement.Add("@UserId");

                        queryParameters.Add("@UserId", parameters.RelatedUserId);

                        // [Type]
                        if (parameters.Document.Type.Received)
                        {
                            dynamicInsertStattement.Add("[type]");
                            dynamicValuesStattement.Add("@Type");
                            queryParameters.Add("@Type", parameters.Document.Type.Value);
                        }

                        // [HouseNumber]
                        if (parameters.Document.IdentifierDocument.Received)
                        {
                            dynamicInsertStattement.Add("[identifier_document]");
                            dynamicValuesStattement.Add("@IdentifierDocument");
                            queryParameters.Add("@IdentifierDocument", parameters.Document.IdentifierDocument.Value);
                        }

                        if (!dynamicSetStattement.Any())
                        {
                            var query = $"UPDATE [documents_users] {string.Join("AND", dynamicSetStattement)} WHERE [user_id] = @UserId";

                            await connection.Connection.ExecuteAsync(
                                new CommandDefinition(
                                        commandText:       query,
                                        parameters:        queryParameters,
                                        transaction:       connection.Transaction,
                                        cancellationToken: contextualizer.CancellationToken,
                                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                        }
                    }
                    else
                    {
                        queryParameters.Add("@UserId", parameters.UserId);

                        // [Type]
                        if (parameters.Document.Type.Received)
                        {
                            dynamicSetStattement.Add("SET [type] = @Type");
                            queryParameters.Add("@Type", parameters.Document.Type.Value);
                        }

                        // [HouseNumber]
                        if (parameters.Document.IdentifierDocument.Received)
                        {
                            dynamicSetStattement.Add("SET [identifier_document] = @IdentifierDocument");
                            queryParameters.Add("@IdentifierDocument", parameters.Document.IdentifierDocument.Value);
                        }

                        if (!dynamicSetStattement.Any())
                        {
                            var query = $"UPDATE [documents_users] {string.Join("AND", dynamicSetStattement)} WHERE [user_id] = @UserId";

                            await connection.Connection.ExecuteAsync(
                                new CommandDefinition(
                                        commandText:       query,
                                        parameters:        queryParameters,
                                        transaction:       connection.Transaction,
                                        cancellationToken: contextualizer.CancellationToken,
                                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                        }
                    }
                }

                // =================== [lawyer] =================== //

                if ((permissionsResult.HasEditLawyerAccountUserPermission || permissionsResult.HasEditAnyLawyerAccountUserPermission) &&
                    ((userInformationResult.HasLawyerAccount.HasValue && userInformationResult.LawyerId.HasValue) && userInformationResult.HasLawyerAccount.Value))
                {
                    // [Lawyer Information]

                    const string queryLawyerInformations = @"
SELECT 

[L].[private] AS [Private], 

CASE WHEN [L].[user_id] = @UserId THEN 1, ELSE 0 END AS [Owner],

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [address_users] [AL] WHERE [AL].[user_id] = @RelatedUserId AND [AL].[lawyer_id] = @LawyerId))
    THEN 1
    ELSE 0
END AS [HasAddress],

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [documents_users] [DL] WHERE [DL].[user_id] = @RelatedUserId AND [DL].[lawyer_id] = @LawyerId))
    THEN 1
    ELSE 0
END AS [HasDocument],

FROM [lawyers] [L] WHERE [L].[user_id] = @RelatedUserId AND [L].[id] = @LawyerId";

                    var queryLawyerInformationParameters = new
                    {
                        RelatedUserId = parameters.RelatedUserId,
                        UserId        = parameters.UserId,
                        LawyerId      = userInformationResult.LawyerId
                    };

                    var lawyerInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner, bool? HasAddress, bool? HasDocument)>(queryLawyerInformations, queryLawyerInformationParameters);

                    // =================== [Table - lawyers] =================== //

                    if (parameters.Accounts.Lawyer.HasChanges)
                    {
                        queryParameters = new DynamicParameters();
                
                        queryParameters.Add("@UserId", parameters.UserId);
                        queryParameters.Add("@LawyerId", userInformationResult.LawyerId.Value);
                
                        dynamicSetStattement.Clear();
                
                        // [Private]
                        if (parameters.Accounts.Lawyer.Private.Received)
                        {
                            dynamicSetStattement.Add("SET [private] = @Private");
                            queryParameters.Add("@Private", parameters.Accounts.Lawyer.Private.Value);
                        }
                
                        // [Phone]
                        if (parameters.Accounts.Lawyer.Phone.Received)
                        {
                            dynamicSetStattement.Add("SET [phone] = @Phone");
                            queryParameters.Add("@Phone", parameters.Accounts.Lawyer.Phone.Value);
                        }
                
                        if (!dynamicSetStattement.Any())
                        {
                            var query = $"UPDATE [lawyers] {string.Join("AND", dynamicSetStattement)} WHERE [id] = @LawyerId AND [user_id] = @UserId";
                
                            var includedItems = await connection.Connection.ExecuteAsync(
                                new CommandDefinition(
                                        commandText:       query,
                                        parameters:        queryParameters,
                                        transaction:       connection.Transaction,
                                        cancellationToken: contextualizer.CancellationToken,
                                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                        }
                    }

                    // =================== [Table - address_lawyers] =================== //
                
                    if (parameters.Accounts.Lawyer.Address.HasChanges)
                    {
                        queryParameters = new DynamicParameters();

                        dynamicInsertStattement.Clear();
                        dynamicValuesStattement.Clear();

                        dynamicSetStattement.Clear();

                        if (!lawyerInformationResult.HasAddress.HasValue || !lawyerInformationResult.HasAddress.Value)
                        {
                            dynamicInsertStattement.Add("[user_id]");
                            dynamicValuesStattement.Add("@UserId");

                            queryParameters.Add("@UserId", parameters.UserId);

                            dynamicInsertStattement.Add("[lawyer_id]");
                            dynamicValuesStattement.Add("@LawyerId");

                            queryParameters.Add("@LawyerId", userInformationResult.LawyerId.Value);

                            // [ZipCode]
                            if (parameters.Address.ZipCode.Received)
                            {
                                dynamicInsertStattement.Add("[zip_code]");
                                dynamicValuesStattement.Add("@ZipCode");
                                queryParameters.Add("@ZipCode", parameters.Address.ZipCode.Value);
                            }

                            // [HouseNumber]
                            if (parameters.Address.HouseNumber.Received)
                            {
                                dynamicInsertStattement.Add("[house_number]");
                                dynamicValuesStattement.Add("@HouseNumber");
                                queryParameters.Add("@HouseNumber", parameters.Address.HouseNumber.Value);
                            }

                            // [Complement]
                            if (parameters.Address.Complement.Received)
                            {
                                dynamicInsertStattement.Add("[complement]");
                                dynamicValuesStattement.Add("@Complement");
                                queryParameters.Add("@Complement", parameters.Address.Complement.Value);
                            }

                            // [District]
                            if (parameters.Address.District.Received)
                            {
                                dynamicInsertStattement.Add("[district]");
                                dynamicValuesStattement.Add("@District");
                                queryParameters.Add("@District", parameters.Address.District.Value);
                            }

                            // [City]
                            if (parameters.Address.City.Received)
                            {
                                dynamicInsertStattement.Add("[city]");
                                dynamicValuesStattement.Add("@City");
                                queryParameters.Add("@City", parameters.Address.City.Value);
                            }

                            // [State]
                            if (parameters.Address.State.Received)
                            {
                                dynamicInsertStattement.Add("[state]");
                                dynamicValuesStattement.Add("@State");
                                queryParameters.Add("@State", parameters.Address.State.Value);
                            }

                            // [Country]
                            if (parameters.Address.Country.Received)
                            {
                                dynamicInsertStattement.Add("[country]");
                                dynamicValuesStattement.Add("@Country");
                                queryParameters.Add("@Country", parameters.Address.Country.Value);
                            }

                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"INSERT INTO [address_lawyers] ({string.Join(",", dynamicInsertStattement)}) VALUES ({string.Join(",", dynamicValuesStattement)})";

                                await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }
                        else
                        {
                            queryParameters = new DynamicParameters();
                
                            queryParameters.Add("@UserId", parameters.UserId);
                            queryParameters.Add("@LawyerId", userInformationResult.LawyerId.Value);

                            dynamicSetStattement.Clear();
                
                            // [ZipCode]
                            if (parameters.Accounts.Lawyer.Address.ZipCode.Received)
                            {
                                dynamicSetStattement.Add("SET [zip_code] = @ZipCode");
                                queryParameters.Add("@ZipCode", parameters.Accounts.Lawyer.Address.ZipCode.Value);
                            }
                
                            // [HouseNumber]
                            if (parameters.Accounts.Lawyer.Address.HouseNumber.Received)
                            {
                                dynamicSetStattement.Add("SET [house_number] = @HouseNumber");
                                queryParameters.Add("@HouseNumber", parameters.Accounts.Lawyer.Address.HouseNumber.Value);
                            }
                
                            // [Complement]
                            if (parameters.Accounts.Lawyer.Address.Complement.Received)
                            {
                                dynamicSetStattement.Add("SET [complement] = @Complement");
                                queryParameters.Add("@Complement", parameters.Accounts.Lawyer.Address.Complement.Value);
                            }
                
                            // [District]
                            if (parameters.Accounts.Lawyer.Address.District.Received)
                            {
                                dynamicSetStattement.Add("SET [district] = @District");
                                queryParameters.Add("@District", parameters.Accounts.Lawyer.Address.District.Value);
                            }
                
                            // [City]
                            if (parameters.Accounts.Lawyer.Address.City.Received)
                            {
                                dynamicSetStattement.Add("SET [city] = @City");
                                queryParameters.Add("@City", parameters.Accounts.Lawyer.Address.City.Value);
                            }
                
                            // [State]
                            if (parameters.Accounts.Lawyer.Address.State.Received)
                            {
                                dynamicSetStattement.Add("SET [state] = @State");
                                queryParameters.Add("@State", parameters.Accounts.Lawyer.Address.State.Value);
                            }
                
                            // [Country]
                            if (parameters.Accounts.Lawyer.Address.Country.Received)
                            {
                                dynamicSetStattement.Add("SET [country] = @Country");
                                queryParameters.Add("@Country", parameters.Accounts.Lawyer.Address.Country.Value);
                            }
                
                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"UPDATE [address_lawyers] {string.Join("AND", dynamicSetStattement)} WHERE [user_id] = @UserId AND [lawyer_id] = @LawyerId";
                
                                var includedItems = await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }                         
                    }

                    // =================== [Table - documents_lawyers] =================== //

                    if (parameters.Accounts.Lawyer.Document.HasChanges)
                    {
                        queryParameters = new DynamicParameters();

                        dynamicInsertStattement.Clear();
                        dynamicValuesStattement.Clear();

                        dynamicSetStattement.Clear();

                        if (!userInformationResult.HasAddress.HasValue || !userInformationResult.HasAddress.Value)
                        {
                            dynamicInsertStattement.Add("[user_id]");
                            dynamicValuesStattement.Add("@UserId");

                            queryParameters.Add("@UserId", parameters.RelatedUserId);

                            dynamicInsertStattement.Add("[lawyer_id]");
                            dynamicValuesStattement.Add("@LawyerId");

                            queryParameters.Add("@LawyerId", userInformationResult.LawyerId);

                            // [Type]
                            if (parameters.Document.Type.Received)
                            {
                                dynamicInsertStattement.Add("[type]");
                                dynamicValuesStattement.Add("@Type");
                                queryParameters.Add("@Type", parameters.Document.Type.Value);
                            }

                            // [HouseNumber]
                            if (parameters.Document.IdentifierDocument.Received)
                            {
                                dynamicInsertStattement.Add("[identifier_document]");
                                dynamicValuesStattement.Add("@IdentifierDocument");
                                queryParameters.Add("@IdentifierDocument", parameters.Document.IdentifierDocument.Value);
                            }

                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"INSERT INTO [documents_lawyers] ({string.Join(",", dynamicInsertStattement)}) VALUES ({string.Join(",", dynamicValuesStattement)})";

                                await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }
                        else
                        {
                            queryParameters.Add("@UserId",   parameters.UserId);
                            queryParameters.Add("@LawyerId", userInformationResult.LawyerId);

                            // [Type]
                            if (parameters.Document.Type.Received)
                            {
                                dynamicSetStattement.Add("SET [type] = @Type");
                                queryParameters.Add("@Type", parameters.Document.Type.Value);
                            }

                            // [HouseNumber]
                            if (parameters.Document.IdentifierDocument.Received)
                            {
                                dynamicSetStattement.Add("SET [identifier_document] = @IdentifierDocument");
                                queryParameters.Add("@IdentifierDocument", parameters.Document.IdentifierDocument.Value);
                            }

                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"UPDATE [documents_lawyers] {string.Join("AND", dynamicSetStattement)} WHERE [user_id] = @UserId AND [lawyer_id] = @LawyerId";

                                await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }
                    }                         
                }
                
                // =================== [CUSTOMER] =================== //

                if ((permissionsResult.HasEditCustomerAccountUserPermission || permissionsResult.HasEditAnyCustomerAccountUserPermission) &&
                   ((userInformationResult.HasCustomerAccount.HasValue && userInformationResult.CustomerId.HasValue) && userInformationResult.HasCustomerAccount.Value))
                {
                    // [Customer Information]

                    const string queryCustomerInformations = @"
SELECT 

[C].[private] AS [Private], 

CASE WHEN [C].[user_id] = @UserId THEN 1, ELSE 0 END AS [Owner],

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [address_users] [AC] WHERE [AC].[user_id] = @RelatedUserId AND [AC].[customer_id] = @CustomerId))
    THEN 1
    ELSE 0
END AS [HasAddress],

SELECT 
CASE 
    WHEN (EXISTS (SELECT 1 FROM [documents_users] [DC] WHERE [DC].[user_id] = @RelatedUserId AND [DC].[customer_id] = @CustomerId))
    THEN 1
    ELSE 0
END AS [HasDocument],

FROM [customers] [C] WHERE [C].[user_id] = @RelatedUserId AND [C].[id] = @CustomerId";

                    var queryCustomerInformationParameters = new
                    {
                        RelatedUserId = parameters.RelatedUserId,
                        UserId        = parameters.UserId,
                        CustomerId    = userInformationResult.CustomerId
                    };

                    var customerInformationResult = await connection.Connection.QueryFirstOrDefaultAsync<(bool? Private, bool? Owner, bool? HasAddress, bool? HasDocument)>(queryCustomerInformations, queryCustomerInformationParameters);

                    // =================== [Table - customers] =================== //

                    if (parameters.Accounts.Customer.HasChanges)
                    {
                        queryParameters = new DynamicParameters();
                
                        queryParameters.Add("@UserId", parameters.UserId);
                        queryParameters.Add("@CustomerId", userInformationResult.CustomerId.Value);
                
                        dynamicSetStattement.Clear();
                
                        // [Private]
                        if (parameters.Accounts.Customer.Private.Received)
                        {
                            dynamicSetStattement.Add("SET [private] = @Private");
                            queryParameters.Add("@Private", parameters.Accounts.Customer.Private.Value);
                        }
                
                        // [Phone]
                        if (parameters.Accounts.Customer.Phone.Received)
                        {
                            dynamicSetStattement.Add("SET [phone] = @Phone");
                            queryParameters.Add("@Phone", parameters.Accounts.Customer.Phone.Value);
                        }
                
                        if (!dynamicSetStattement.Any())
                        {
                            var query = $"UPDATE [customers] {string.Join("AND", dynamicSetStattement)} WHERE [id] = @CustomerId AND [user_id] = @UserId";
                
                            var includedItems = await connection.Connection.ExecuteAsync(
                                new CommandDefinition(
                                        commandText:       query,
                                        parameters:        queryParameters,
                                        transaction:       connection.Transaction,
                                        cancellationToken: contextualizer.CancellationToken,
                                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                        }
                    }

                    // =================== [Table - address_customers] =================== //
                
                    if (parameters.Accounts.Lawyer.Address.HasChanges)
                    {
                         queryParameters = new DynamicParameters();

                        dynamicInsertStattement.Clear();
                        dynamicValuesStattement.Clear();

                        dynamicSetStattement.Clear();

                        if (!customerInformationResult.HasAddress.HasValue || !customerInformationResult.HasAddress.Value)
                        {
                            dynamicInsertStattement.Add("[user_id]");
                            dynamicValuesStattement.Add("@UserId");

                            queryParameters.Add("@UserId", parameters.UserId);

                            dynamicInsertStattement.Add("[customer_id]");
                            dynamicValuesStattement.Add("@CustomerId");

                            queryParameters.Add("@CustomerId", userInformationResult.CustomerId.Value);

                            // [ZipCode]
                            if (parameters.Address.ZipCode.Received)
                            {
                                dynamicInsertStattement.Add("[zip_code]");
                                dynamicValuesStattement.Add("@ZipCode");
                                queryParameters.Add("@ZipCode", parameters.Address.ZipCode.Value);
                            }

                            // [HouseNumber]
                            if (parameters.Address.HouseNumber.Received)
                            {
                                dynamicInsertStattement.Add("[house_number]");
                                dynamicValuesStattement.Add("@HouseNumber");
                                queryParameters.Add("@HouseNumber", parameters.Address.HouseNumber.Value);
                            }

                            // [Complement]
                            if (parameters.Address.Complement.Received)
                            {
                                dynamicInsertStattement.Add("[complement]");
                                dynamicValuesStattement.Add("@Complement");
                                queryParameters.Add("@Complement", parameters.Address.Complement.Value);
                            }

                            // [District]
                            if (parameters.Address.District.Received)
                            {
                                dynamicInsertStattement.Add("[district]");
                                dynamicValuesStattement.Add("@District");
                                queryParameters.Add("@District", parameters.Address.District.Value);
                            }

                            // [City]
                            if (parameters.Address.City.Received)
                            {
                                dynamicInsertStattement.Add("[city]");
                                dynamicValuesStattement.Add("@City");
                                queryParameters.Add("@City", parameters.Address.City.Value);
                            }

                            // [State]
                            if (parameters.Address.State.Received)
                            {
                                dynamicInsertStattement.Add("[state]");
                                dynamicValuesStattement.Add("@State");
                                queryParameters.Add("@State", parameters.Address.State.Value);
                            }

                            // [Country]
                            if (parameters.Address.Country.Received)
                            {
                                dynamicInsertStattement.Add("[country]");
                                dynamicValuesStattement.Add("@Country");
                                queryParameters.Add("@Country", parameters.Address.Country.Value);
                            }

                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"INSERT INTO [address_customers] ({string.Join(",", dynamicInsertStattement)}) VALUES ({string.Join(",", dynamicValuesStattement)})";

                                await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }
                        else
                        {
                            queryParameters = new DynamicParameters();
                
                            queryParameters.Add("@UserId", parameters.UserId);
                            queryParameters.Add("@CustomerId", userInformationResult.CustomerId.Value);

                            dynamicSetStattement.Clear();
                
                            // [ZipCode]
                            if (parameters.Accounts.Lawyer.Address.ZipCode.Received)
                            {
                                dynamicSetStattement.Add("SET [zip_code] = @ZipCode");
                                queryParameters.Add("@ZipCode", parameters.Accounts.Lawyer.Address.ZipCode.Value);
                            }
                
                            // [HouseNumber]
                            if (parameters.Accounts.Lawyer.Address.HouseNumber.Received)
                            {
                                dynamicSetStattement.Add("SET [house_number] = @HouseNumber");
                                queryParameters.Add("@HouseNumber", parameters.Accounts.Lawyer.Address.HouseNumber.Value);
                            }
                
                            // [Complement]
                            if (parameters.Accounts.Lawyer.Address.Complement.Received)
                            {
                                dynamicSetStattement.Add("SET [complement] = @Complement");
                                queryParameters.Add("@Complement", parameters.Accounts.Lawyer.Address.Complement.Value);
                            }
                
                            // [District]
                            if (parameters.Accounts.Lawyer.Address.District.Received)
                            {
                                dynamicSetStattement.Add("SET [district] = @District");
                                queryParameters.Add("@District", parameters.Accounts.Lawyer.Address.District.Value);
                            }
                
                            // [City]
                            if (parameters.Accounts.Lawyer.Address.City.Received)
                            {
                                dynamicSetStattement.Add("SET [city] = @City");
                                queryParameters.Add("@City", parameters.Accounts.Lawyer.Address.City.Value);
                            }
                
                            // [State]
                            if (parameters.Accounts.Lawyer.Address.State.Received)
                            {
                                dynamicSetStattement.Add("SET [state] = @State");
                                queryParameters.Add("@State", parameters.Accounts.Lawyer.Address.State.Value);
                            }
                
                            // [Country]
                            if (parameters.Accounts.Lawyer.Address.Country.Received)
                            {
                                dynamicSetStattement.Add("SET [country] = @Country");
                                queryParameters.Add("@Country", parameters.Accounts.Lawyer.Address.Country.Value);
                            }
                
                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"UPDATE [address_customers] {string.Join("AND", dynamicSetStattement)} WHERE [user_id] = @UserId AND [customer_id] = @CustomerId";
                
                                var includedItems = await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }     
                    }
                
                    // =================== [Table - documents_customers] =================== //
                
                    if (parameters.Accounts.Customer.Document.HasChanges)
                    {
                        queryParameters = new DynamicParameters();

                        dynamicInsertStattement.Clear();
                        dynamicValuesStattement.Clear();

                        dynamicSetStattement.Clear();

                        if (!userInformationResult.HasAddress.HasValue || !userInformationResult.HasAddress.Value)
                        {
                            dynamicInsertStattement.Add("[user_id]");
                            dynamicValuesStattement.Add("@UserId");

                            queryParameters.Add("@UserId", parameters.RelatedUserId);

                            dynamicInsertStattement.Add("[customer_id]");
                            dynamicValuesStattement.Add("@CustomerId");

                            queryParameters.Add("@CustomerId", userInformationResult.CustomerId);

                            // [Type]
                            if (parameters.Document.Type.Received)
                            {
                                dynamicInsertStattement.Add("[type]");
                                dynamicValuesStattement.Add("@Type");
                                queryParameters.Add("@Type", parameters.Document.Type.Value);
                            }

                            // [HouseNumber]
                            if (parameters.Document.IdentifierDocument.Received)
                            {
                                dynamicInsertStattement.Add("[identifier_document]");
                                dynamicValuesStattement.Add("@IdentifierDocument");
                                queryParameters.Add("@IdentifierDocument", parameters.Document.IdentifierDocument.Value);
                            }

                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"INSERT INTO [documents_customers] ({string.Join(",", dynamicInsertStattement)}) VALUES ({string.Join(",", dynamicValuesStattement)})";

                                await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }
                        else
                        {
                            queryParameters.Add("@UserId",   parameters.UserId);
                            queryParameters.Add("@CustomerId", userInformationResult.CustomerId);

                            // [Type]
                            if (parameters.Document.Type.Received)
                            {
                                dynamicSetStattement.Add("SET [type] = @Type");
                                queryParameters.Add("@Type", parameters.Document.Type.Value);
                            }

                            // [HouseNumber]
                            if (parameters.Document.IdentifierDocument.Received)
                            {
                                dynamicSetStattement.Add("SET [identifier_document] = @IdentifierDocument");
                                queryParameters.Add("@IdentifierDocument", parameters.Document.IdentifierDocument.Value);
                            }

                            if (!dynamicSetStattement.Any())
                            {
                                var query = $"UPDATE [documents_customers] {string.Join("AND", dynamicSetStattement)} WHERE [user_id] = @UserId AND [customer_id] = @CustomerId";

                                await connection.Connection.ExecuteAsync(
                                    new CommandDefinition(
                                            commandText:       query,
                                            parameters:        queryParameters,
                                            transaction:       connection.Transaction,
                                            cancellationToken: contextualizer.CancellationToken,
                                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
                            }
                        }
                    }
                }
            },
            transactionOptions: new() { ExecuteRollbackAndCommit = true });

        return resultConstructor.Build();
    }

    public async Task<Result> GrantPermissionsAsync(GrantPermissionsParameters parameters, Contextualizer contextualizer)
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
            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            GrantPermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_USER, contextualizer),
            GrantPermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            GrantPermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_USER, contextualizer),
            GrantPermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            GrantPermissionsAnyUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_USER, contextualizer),
            GrantPermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),
        };

        //var isUserOwnerOfTheRelatedUser = parameters.RelatedUserId == parameters.UserId;

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

        // [Related User Id]
        var relatedUserIdResult = await ValidateUserId(
            parameters.RelatedUserId,
            contextualizer);

        if (relatedUserIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(relatedUserIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build().Incorporate(attributeAccountResult);

        // [Permission Validation]

        const string queryPermissions = @"
SELECT

/* ---------------------------------------------- [GRANT_PERMISSIONS_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [GRANT_PERMISSIONS_OWN_USER]

    (@GrantPermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                             AND
            [PGU].[permission_id] = @GrantPermissionsOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                             AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [GRANT_PERMISSIONS_OWN_USER]

    (@GrantPermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                             AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsOwnUserPermission],

/* ---------------------------------------------- [GRANT_PERMISSION_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [GRANT_PERMISSION_USER]

    (@GrantPermissionUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                   AND
            [PGR].[user_id]         = @UserId                          AND
            [PGR].[permission_id]   = @GrantPermissionUserPermissionId AND
            [PGR].[role_id]         = @RoleId                          AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsUserPermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_ANY_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [GRANT_PERMISSIONS_ANY_USER]

    (@GrantPermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                              AND
            [PGU].[permission_id] = @GrantPermissionsAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                              AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [GRANT_PERMISSIONS_ANY_USER]

    (@GrantPermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                              AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsAnyUserPermission],

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

/* ---------------------------------------------- [VIEW_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [VIEW_USER]

    (@ViewUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId        AND
            [PGR].[user_id]         = @UserId               AND
            [PGR].[permission_id]   = @ViewUserPermissionId AND
            [PGR].[role_id]         = @RoleId               AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewUserPermission]";

        var queryPermissionsParameters = new
        {
            GrantPermissionsUserPermissionId    = permission.GrantPermissionsUserPermissionId,
            GrantPermissionOwnUserPermissionId  = permission.GrantPermissionsOwnUserPermissionId,
            GrantPermissionsAnyUserPermissionId = permission.GrantPermissionsAnyUserPermissionId,

            ViewOwnUserPermissionId    = permission.ViewOwnUserPermissionId,
            ViewAnyUserPermissionId    = permission.ViewAnyUserPermissionId,
            ViewPublicUserPermissionId = permission.ViewPublicUserPermissionId,
            ViewUserPermissionId       = permission.ViewUserPermissionId,

            UserId        = parameters.UserId,
            RelatedUserId = parameters.RelatedUserId,
            AttributeId   = parameters.AttributeId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.GrantPermissions>(queryPermissions, queryPermissionsParameters);

        // [User Information]

        const string queryUserInformations = @"
SELECT 

[U].[private] AS [Private], 

CASE WHEN [U].[id] = @UserId THEN 1, ELSE 0 END AS [Owner],

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
            resultConstructor.SetConstructor(new GrantPermissionDeniedError());

            return resultConstructor.Build();
        }

        var distinctPermission = parameters.Permissions.Distinct();

        var internalValues = new InternalValues.GrantPermission()
        {
            Data = new()
            {
                Items = distinctPermission
                    .Distinct()
                    .ToDictionary(
                        x => x.Id,
                        x => new InternalValues.GrantPermission.DataPropreties.Item()
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

            if(result.IsFinished)
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

        // [Check Permission Objects Permissions]

        const string queryPermissionsSpecificUser = @"
/* ---------------------------------------------- [GRANT_PERMISSIONS_ANY_USER] ---------------------------------------------- */

SELECT

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [GRANT_PERMISSIONS_ANY_USER]

    (@GrantPermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                              AND
            [PGU].[permission_id] = @GrantPermissionsAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                              AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [GRANT_PERMISSIONS_ANY_USER]

    (@GrantPermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                              AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsAnyUserPermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [GRANT_PERMISSION_ANY_LAWYER_ACCOUNT_USER]

    (@GrantPermissionsAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                           AND
            [PGU].[permission_id] = @GrantPermissionsAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                           AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

    (@GrantPermissionsAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                           AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER]

    (@GrantPermissionsAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                             AND
            [PGU].[permission_id] = @GrantPermissionsAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                             AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [GRANT_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER]

    (@GrantPermissionsAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                             AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsAnyCustomerAccountUserPermission],


/* ---------------------------------------------- [GRANT_PERMISSIONS_OWN_USER] ---------------------------------------------- */

SELECT

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [GRANT_PERMISSIONS_OWN_USER]

    (@GrantPermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                              AND
            [PGU].[permission_id] = @GrantPermissionsOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                              AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [GRANT_PERMISSIONS_OWN_USER]

    (@GrantPermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                              AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsOwnUserPermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER]

    (@GrantPermissionsOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                           AND
            [PGU].[permission_id] = @GrantPermissionsOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                           AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [GRANT_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER]

    (@GrantPermissionsOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                           AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsOwnLawyerAccountUserPermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER]

    (@GrantPermissionsOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                             AND
            [PGU].[permission_id] = @GrantPermissionsOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                             AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [GRANT_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER]

    (@GrantPermissionsOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                             AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsOwnCustomerAccountUserPermission],

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
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCustomerAccountUserPermission],

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
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                  AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                  AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCustomerAccountUserPermission]";

        var queryPermissionsParametersSpecificUser = new 
        { 
            ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
            ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,            
            ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

            GrantPermissionsAnyUserPermissionId                = permission.GrantPermissionsAnyUserPermissionId,
            GrantPermissionsAnyLawyerAccountUserPermissionId   = permission.GrantPermissionsAnyLawyerAccountUserPermissionId,
            GrantPermissionsAnyCustomerAccountUserPermissionId = permission.GrantPermissionsAnyCustomerAccountUserPermissionId,
            
            GrantPermissionsOwnUserPermissionId                = permission.GrantPermissionsOwnUserPermissionId,
            GrantPermissionsOwnLawyerAccountUserPermissionId   = permission.GrantPermissionsOwnLawyerAccountUserPermissionId,
            GrantPermissionsOwnCustomerAccountUserPermissionId = permission.GrantPermissionsOwnCustomerAccountUserPermissionId,

            ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
            ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
            ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

            AttributeId = parameters.UserId,
            UserId      = parameters.UserId,
            RoleId      = parameters.RoleId
        };

        var permissionsResultSpecificUser = await connection.Connection.QueryFirstAsync<PermissionResult.GrantPermissions.SpecificUser>(queryPermissionsSpecificUser, queryPermissionsParametersSpecificUser);

        const string queryAttributes = "SELECT [A].[id] AS [Id], [A].[name] AS [Name] FROM [Attributes]";

        var attributes = await connection.Connection.QueryAsync<(int Id, string Name)>(queryAttributes);

        const string queryAllowedPermissions = @"
SELECT [P].[id] AS [Id] FROM [Permissions] WHERE [P].[name] IN 
(GRANT_PERMISSIONS_USER, REVOKE_PERMISSIONS_USER, GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER, REVOKE_PERMISSIONS_USER, CHAT_USER, VIEW_USER, VIEW_LAWYER_ACCOUNT_USER, VIEW_CUSTOMER_ACCOUNT_USER, EDIT_USER)";

        var allowedPermissions = await connection.Connection.QueryAsync<int>(queryAllowedPermissions);

        foreach (var item in internalValues.Data.Items.Values)
        {
            var resultContructor = new ResultConstructor();

            if (!allowedPermissions.Contains(item.PermissionId))
            {
                resultContructor.SetConstructor(new ForbiddenPermissionToGrantError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }

            var hasPermissionToAssignUser = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    // [NOT ACL]

                    HasGrantPermissionsAnyUserPermission                = permissionsResultSpecificUser.HasGrantPermissionsAnyUserPermission,
                    HasGrantPermissionsAnyLawyerAccountUserPermission   = permissionsResultSpecificUser.HasGrantPermissionsAnyLawyerAccountUserPermission,
                    HasGrantPermissionsAnyCustomerAccountUserPermission = permissionsResultSpecificUser.HasGrantPermissionsAnyCustomerAccountUserPermission,

                    HasGrantPermissionsOwnUserPermission                = permissionsResultSpecificUser.HasGrantPermissionsOwnUserPermission,
                    HasGrantPermissionsOwnLawyerAccountUserPermission   = permissionsResultSpecificUser.HasGrantPermissionsOwnLawyerAccountUserPermission,
                    HasGrantPermissionsOwnCustomerAccountUserPermission = permissionsResultSpecificUser.HasGrantPermissionsOwnCustomerAccountUserPermission,

                    HasViewAnyUserPermission                = permissionsResultSpecificUser.HasViewAnyUserPermission,
                    HasViewAnyLawyerAccountUserPermission   = permissionsResultSpecificUser.HasViewAnyLawyerAccountUserPermission,
                    HasViewAnyCustomerAccountUserPermission = permissionsResultSpecificUser.HasViewAnyCustomerAccountUserPermission,

                    HasViewPublicUserPermission                = permissionsResultSpecificUser.HasViewPublicUserPermission,
                    HasViewPublicLawyerAccountUserPermission   = permissionsResultSpecificUser.HasViewPublicLawyerAccountUserPermission,
                    HasViewPublicCustomerAccountUserPermission = permissionsResultSpecificUser.HasViewPublicCustomerAccountUserPermission,
                    // [ACL]

                    ViewUserPermissionId                = permission.ViewUserPermissionId,
                    ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                    ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,

                    GrantPermissionsUserPermissionId                = permission.GrantPermissionsUserPermissionId,
                    GrantPermissionsLawyerAccountUserPermissionId   = permission.GrantPermissionsLawyerAccountUserPermissionId,
                    GrantPermissionsCustomerAccountUserPermissionId = permission.GrantPermissionsUserPermissionId,

                    Attribute = attributes.First(x => x.Id == item.AttributeId),

                    AttributeId  = item.AttributeId,
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
                            LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                            WHERE
                                [PGRu].[related_user_id] = @UserId               AND 
                                [PGRu].[user_id]         = @ExternalUserId       AND 
                                [PGRu].[role_id]         = @RoleId               AND 
                                [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                                ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Al] ON [Al].[id] = [PGRl].[attribute_id]
                                        WHERE
                                            [PGRl].[related_user_id] = @UserId                            AND
                                            [PGRl].[user_id]         = @ExternalUserId                    AND
                                            [PGRl].[role_id]         = @RoleId                            AND
                                            [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
                                            ([PGRl].[attribute_id] IS NULL OR [Al].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Ac] ON [Ac].[id] = [PGRc].[attribute_id]
                                        WHERE
                                            [PGRc].[related_user_id] = @UserId                             AND 
                                            [PGRc].[user_id]         = @ExternalUserId                      AND 
                                            [PGRc].[role_id]         = @RoleId                              AND 
                                            [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                            ([PGRc].[attribute_id] IS NULL OR [Ac].[id] = @AttributeId)
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
        END AS apply
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
                        LEFT JOIN [attributes] [Au2] ON [Au2].[id] = [PGRu2].[attribute_id]
                        WHERE
                            [PGRu2].[related_user_id] = @UserId                           AND 
                            [PGRu2].[user_id]         = @ExternalUserId                   AND 
                            [PGRu2].[role_id]         = @RoleId                           AND 
                            [PGRu2].[permission_id]   = @GrantPermissionsUserPermissionId AND 
                            ([PGRu2].[attribute_id] IS NULL OR [Au2].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Au2] ON [Au2].[id] = [PGRu2].[attribute_id]
                                        WHERE
                                            [PGRu2].[related_user_id] = @UserId                                        AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                AND 
                                            [PGRu2].[role_id]         = @RoleId                                        AND 
                                            [PGRu2].[permission_id]   = @GrantPermissionsLawyerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [Au2].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Au2] ON [Au2].[id] = [PGRu2].[attribute_id]
                                        WHERE
                                            [PGRu2].[related_user_id] = @UserId                                          AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                  AND 
                                            [PGRu2].[role_id]         = @RoleId                                          AND 
                                            [PGRu2].[permission_id]   = @GrantPermissionsCustomerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [Au2].[id] = @AttributeId)
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
                resultContructor.SetConstructor(new GrantPermissionsForSpecificUserDeniedError()
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
            resultConstructor.SetConstructor(new GrantPermissionSuccess()
            {
                Details = new()
                {
                    IncludedItems = 0,

                    Result = internalValues.Data.Finished.Values.Select((item) =>
                        new GrantPermissionSuccess.DetailsVariation.Fields
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
                RelatedUserId = parameters.RelatedUserId,

                AttributeId  = x.AttributeId,
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

        resultConstructor.SetConstructor(new GrantPermissionSuccess()
        {
            Details = new()
            {
                IncludedItems = includedItems,

                Result = internalValues.Data.Finished.Values.Select((item) =>
                    new GrantPermissionSuccess.DetailsVariation.Fields
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

    public async Task<Result> RevokePermissionsAsync(RevokePermissionsParameters parameters, Contextualizer contextualizer)
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

            RevokePermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_USER, contextualizer),
            RevokePermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            RevokePermissionsOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_USER, contextualizer),
            RevokePermissionsOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_LAWYER_ACCOUNT_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),

            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_LAWYER_ACCOUNT_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            RevokePermissionsAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
            RevokePermissionsAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_LAWYER_ACCOUNT_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
        };

        //var isUserOwnerOfTheRelatedUser = parameters.RelatedUserId == parameters.UserId;

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

        // [Related User Id]
        var relatedUserIdResult = await ValidateUserId(
            parameters.RelatedUserId,
            contextualizer);

        if (relatedUserIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(relatedUserIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build().Incorporate(attributeAccountResult);

      // [Permission Validation]

        const string queryPermissions = @"
SELECT

/* ---------------------------------------------- [REVOKE_PERMISSIONS_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [REVOKE_PERMISSIONS_OWN_USER]

    (@RevokePermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @RevokePermissionsOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [REVOKE_PERMISSIONS_OWN_USER]

    (@RevokePermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsOwnUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationships (ACL Grant)] [REVOKE_PERMISSIONS_USER]

    (@RevokePermissionsUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationships] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                     AND
            [PGR].[user_id]         = @UserId                            AND
            [PGR].[permission_id]   = @RevokePermissionsUserPermissionId AND
            [PGR].[role_id]         = @RoleId                            AND
            ([PGC].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_ANY_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [REVOKE_PERMISSIONS_ANY_USER]

    (@RevokePermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @RevokePermissionsAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_ANY_USER]

    (@RevokePermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsAnyUserPermission],

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

/* ---------------------------------------------- [VIEW_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [VIEW_USER]

    (@ViewUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        LEFT JOIN [attributes] [A_PGR] ON [PGR].[attribute_id] = [A_PGR].[id]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId        AND
            [PGR].[user_id]         = @UserId               AND
            [PGR].[permission_id]   = @ViewUserPermissionId AND
            [PGR].[role_id]         = @RoleId               AND
            ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewUserPermission]";

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
            AttributeId   = parameters.AttributeId,
            RoleId        = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.RevokePermissions>(queryPermissions, queryPermissionsParameters);

        // [User Information]

        const string queryUserInformations = @"
SELECT 

[U].[private] AS [Private], 

CASE WHEN [U].[id] = @UserId THEN 1, ELSE 0 END AS [Owner],

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
            resultConstructor.SetConstructor(new RevokePermissionDeniedError());

            return resultConstructor.Build();
        }
        // [Permission Objects Validations]

        var distinctPermission = parameters.Permissions.Distinct();

        var internalValues = new InternalValues.RevokePermission()
        {
            Data = new()
            {
                Items = distinctPermission
                    .Distinct()
                    .ToDictionary(
                        x => x.Id,
                        x => new InternalValues.RevokePermission.DataPropreties.Item()
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

            if(result.IsFinished)
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

        // [Check Permission Objects Permissions]

        const string queryPermissionsSpecificUser = @"
/* ---------------------------------------------- [REVOKE_PERMISSIONS_ANY_USER] ---------------------------------------------- */

SELECT

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [REVOKE_PERMISSIONS_ANY_USER]

    (@RevokePermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @RevokePermissionsAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_ANY_USER]

    (@RevokePermissionsAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsAnyUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

    (@RevokePermissionsAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                            AND
            [PGU].[permission_id] = @RevokePermissionsAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                            AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

    (@RevokePermissionsAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                            AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER]

    (@RevokePermissionsAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                              AND
            [PGU].[permission_id] = @RevokePermissionsAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                              AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_ANY_CUSTOMER_ACCOUNT_USER]

    (@RevokePermissionsAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                              AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsAnyCustomerAccountUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_OWN_USER] ---------------------------------------------- */

SELECT

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [REVOKE_PERMISSIONS_OWN_USER]

    (@RevokePermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @RevokePermissionsOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_OWN_USER]

    (@RevokePermissionsOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsOwnUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

    (@RevokePermissionsOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                            AND
            [PGU].[permission_id] = @RevokePermissionsOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                            AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

    (@RevokePermissionsOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                            AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsOwnLawyerAccountUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER]

    (@RevokePermissionsOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                              AND
            [PGU].[permission_id] = @RevokePermissionsOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                              AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_OWN_CUSTOMER_ACCOUNT_USER]

    (@RevokePermissionsOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                              AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsOwnCustomerAccountUserPermission],

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
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_ANY_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                 AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId                                 AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCustomerAccountUserPermission],

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
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                  AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                  AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                    AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                    AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCustomerAccountUserPermission]";

        var queryPermissionsParametersSpecificUser = new 
        { 
            ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
            ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,            
            ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

            RevokePermissionsAnyUserPermissionId                = permission.RevokePermissionsAnyUserPermissionId,
            RevokePermissionsAnyLawyerAccountUserPermissionId   = permission.RevokePermissionsAnyLawyerAccountUserPermissionId,
            RevokePermissionsAnyCustomerAccountUserPermissionId = permission.RevokePermissionsAnyCustomerAccountUserPermissionId,
            
            RevokePermissionsOwnUserPermissionId                = permission.RevokePermissionsOwnUserPermissionId,
            RevokePermissionsOwnLawyerAccountUserPermissionId   = permission.RevokePermissionsOwnLawyerAccountUserPermissionId,
            RevokePermissionsOwnCustomerAccountUserPermissionId = permission.RevokePermissionsOwnCustomerAccountUserPermissionId,
            
            ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
            ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
            ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

            AttributeId = parameters.UserId,
            UserId      = parameters.UserId,
            RoleId      = parameters.RoleId
        };

        var permissionsResultSpecificUser = await connection.Connection.QueryFirstAsync<PermissionResult.RevokePermissions.SpecificUser>(queryPermissionsSpecificUser, queryPermissionsParametersSpecificUser);

        const string queryAttributes = "SELECT [A].[id] AS [Id], [A].[name] AS [Name] FROM [Attributes]";

        var attributes = await connection.Connection.QueryAsync<(int Id, string Name)>(queryAttributes);

        const string queryAllowedPermissions = @"
SELECT [P].[id] AS [Id] FROM [Permissions] WHERE [P].[name] IN 
(GRANT_PERMISSIONS_USER, REVOKE_PERMISSIONS_USER, GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER, REVOKE_PERMISSIONS_USER, CHAT_USER, VIEW_USER, VIEW_LAWYER_ACCOUNT_USER, VIEW_CUSTOMER_ACCOUNT_USER, EDIT_USER)";

        var allowedPermissions = await connection.Connection.QueryAsync<int>(queryAllowedPermissions);

        foreach (var item in internalValues.Data.Items.Values)
        {
            var resultContructor = new ResultConstructor();

            if (!allowedPermissions.Contains(item.PermissionId))
            {
                resultContructor.SetConstructor(new ForbiddenPermissionToRevokeError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }

            var hasPermissionToAssignUser = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    // [NOT ACL]

                    HasRevokePermissionsAnyUserPermission                = permissionsResultSpecificUser.HasRevokePermissionsAnyUserPermission,
                    HasRevokePermissionsAnyLawyerAccountUserPermission   = permissionsResultSpecificUser.HasRevokePermissionsAnyLawyerAccountUserPermission,
                    HasRevokePermissionsAnyCustomerAccountUserPermission = permissionsResultSpecificUser.HasRevokePermissionsAnyCustomerAccountUserPermission,

                    HasViewAnyUserPermission                = permissionsResultSpecificUser.HasViewAnyUserPermission,
                    HasViewAnyLawyerAccountUserPermission   = permissionsResultSpecificUser.HasViewAnyLawyerAccountUserPermission,
                    HasViewAnyCustomerAccountUserPermission = permissionsResultSpecificUser.HasViewAnyCustomerAccountUserPermission,

                    HasViewPublicUserPermission                = permissionsResultSpecificUser.HasViewPublicUserPermission,
                    HasViewPublicLawyerAccountUserPermission   = permissionsResultSpecificUser.HasViewPublicLawyerAccountUserPermission,
                    HasViewPublicCustomerAccountUserPermission = permissionsResultSpecificUser.HasViewPublicCustomerAccountUserPermission,
                    // [ACL]

                    ViewUserPermissionId                = permission.ViewUserPermissionId,
                    ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                    ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,

                    RevokePermissionsUserPermissionId                = permission.RevokePermissionsUserPermissionId,
                    RevokePermissionsLawyerAccountUserPermissionId   = permission.RevokePermissionsLawyerAccountUserPermissionId,
                    RevokePermissionsCustomerAccountUserPermissionId = permission.RevokePermissionsUserPermissionId,

                    Attribute = attributes.First(x => x.Id == item.AttributeId),

                    AttributeId  = item.AttributeId,
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
                            LEFT JOIN [attributes] [Au] ON [Au].[id] = [PGRu].[attribute_id]
                            WHERE
                                [PGRu].[related_user_id] = @UserId               AND 
                                [PGRu].[user_id]         = @ExternalUserId       AND 
                                [PGRu].[role_id]         = @RoleId               AND 
                                [PGRu].[permission_id]   = @ViewUserPermissionId AND 
                                ([PGRu].[attribute_id] IS NULL OR [Au].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Al] ON [Al].[id] = [PGRl].[attribute_id]
                                        WHERE
                                            [PGRl].[related_user_id] = @UserId                            AND
                                            [PGRl].[user_id]         = @ExternalUserId                    AND
                                            [PGRl].[role_id]         = @RoleId                            AND
                                            [PGRl].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
                                            ([PGRl].[attribute_id] IS NULL OR [Al].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Ac] ON [Ac].[id] = [PGRc].[attribute_id]
                                        WHERE
                                            [PGRc].[related_user_id] = @UserId                              AND 
                                            [PGRc].[user_id]         = @ExternalUserId                      AND 
                                            [PGRc].[role_id]         = @RoleId                              AND 
                                            [PGRc].[permission_id]   = @ViewCustomerAccountUserPermissionId AND 
                                            ([PGRc].[attribute_id] IS NULL OR [Ac].[id] = @AttributeId)
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
        END AS apply
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
                        LEFT JOIN [attributes] [Au2] ON [Au2].[id] = [PGRu2].[attribute_id]
                        WHERE
                            [PGRu2].[related_user_id] = @UserId                            AND 
                            [PGRu2].[user_id]         = @ExternalUserId                    AND 
                            [PGRu2].[role_id]         = @RoleId                            AND 
                            [PGRu2].[permission_id]   = @RevokePermissionsUserPermissionId AND 
                            ([PGRu2].[attribute_id] IS NULL OR [Au2].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Au2] ON [Au2].[id] = [PGRu2].[attribute_id]
                                        WHERE
                                            [PGRu2].[related_user_id] = @UserId                                         AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                 AND 
                                            [PGRu2].[role_id]         = @RoleId                                         AND 
                                            [PGRu2].[permission_id]   = @RevokePermissionsLawyerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [Au2].[id] = @AttributeId)
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
                                        LEFT JOIN [attributes] [Au2] ON [Au2].[id] = [PGRu2].[attribute_id]
                                        WHERE
                                            [PGRu2].[related_user_id] = @UserId                                           AND 
                                            [PGRu2].[user_id]         = @ExternalUserId                                   AND 
                                            [PGRu2].[role_id]         = @RoleId                                           AND 
                                            [PGRu2].[permission_id]   = @RevokePermissionsCustomerAccountUserPermissionId AND 
                                            ([PGRu2].[attribute_id] IS NULL OR [Au2].[id] = @AttributeId)
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
                resultContructor.SetConstructor(new RevokePermissionsForSpecificUserDeniedError());

                internalValues.Data.Items[item.Id].Result = resultContructor.Build();

                internalValues.Data.Finish(item.Id);

                continue;
            }
        }

        if (!internalValues.Data.Items.Any())
        {
            resultConstructor.SetConstructor(new RevokePermissionSuccess()
            {
                Details = new()
                {
                    DeletedItems = 0,

                    Result = internalValues.Data.Finished.Values.Select((item) =>
                        new RevokePermissionSuccess.DetailsVariation.Fields
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
            var items = internalValues.Data.Items.Values.Select(x => new
            {
                RelatedUserId = parameters.RelatedUserId,

                AttributeId  = x.AttributeId,
                PermissionId = x.PermissionId,
                UserId       = x.UserId,
                RoleId       = x.RoleId
            });

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"DELETE [permission_grants_relationship] WHERE [related_user_id] = @RelatedUserId AND [user_id] = @UserId AND [permission_id] = @PermissionId AND [attribute_id] = @AttributeId;");

            var deletedItems = await connection.Connection.ExecuteAsync(stringBuilder.ToString(), items);
            return deletedItems;
        });

        resultConstructor.SetConstructor(new RevokePermissionSuccess()
        {
            Details = new()
            {
                DeletedItems = deletedItems,

                Result = internalValues.Data.Finished.Values.Select((item) =>
                    new RevokePermissionSuccess.DetailsVariation.Fields
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