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

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<SearchInformation>().Incorporate(roleIdResult);

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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
                        @ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
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
                        WHERE
                            [PGRu].[related_user_id] = @UserId                AND 
                            [PGRu].[user_id]         = @ExternalUserId        AND 
                            [PGRu].[role_id]         = @RoleId                AND 
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

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<CountInformation>().Incorporate(roleIdResult);

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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
    )) THEN 1
    ELSE 0
END AS [HasViewPublicUserPermission]";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId    = permission.ViewOwnUserPermissionId,               
                ViewPublicUserPermissionId = permission.ViewPublicUserPermissionId,               
                ViewAnyUserPermissionId    = permission.ViewAnyUserPermissionId,

                UserId      = parameters.UserId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Count>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                 // [NOT ACL]

                 HasViewOwnUserPermission    = permissionsResult.HasViewOwnUserPermission,
                 HasViewAnyUserPermission    = permissionsResult.HasViewAnyUserPermission,
                 HasViewPublicUserPermission = permissionsResult.HasViewPublicUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 UserId = parameters.UserId,
                 RoleId = parameters.RoleId
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

        // [Role Id]
        var roleIdResult = await ValidateRoleId(
            parameters.RoleId,
            contextualizer);

        if (roleIdResult.IsFinished)
            return resultConstructor.Build<DetailsInformation>().Incorporate(roleIdResult);

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

        var result = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
SELECT

/* ---------------------------------------------- [VIEW_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_relationship (ACL Grant)] [VIEW_USER]

    (@ViewUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_relationship] [PGR]
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId        AND
            [PGR].[user_id]         = @UserId               AND
            [PGR].[permission_id]   = @ViewUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                     AND
            [PGR].[user_id]         = @UserId                            AND
            [PGR].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                       AND
            [PGR].[user_id]         = @UserId                              AND
            [PGR].[permission_id]   = @ViewCustomerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId
    )) THEN 1
    ELSE 0
END AS [HasViewCustomerAccountUserPermission],

/* ---------------------------------------------- [VIEW_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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

                ViewUserPermissionId                = permission.ViewUserPermissionId,
                ViewLawyerAccountUserPermissionId   = permission.ViewLawyerAccountUserPermissionId,
                ViewCustomerAccountUserPermissionId = permission.ViewCustomerAccountUserPermissionId,

                UserId      = parameters.UserId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Details>(queryPermissions, queryPermissionsParameters);

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

                return resultConstructor.Build<DetailsInformation>();
            }

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
                 
                 HasViewUserPermission                = permissionsResult.HasViewUserPermission,
                 HasViewLawyerAccountUserPermission   = permissionsResult.HasViewLawyerAccountUserPermission,
                 HasViewCustomerAccountUserPermission = permissionsResult.HasViewCustomerAccountUserPermission,

                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                 ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
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
                ([U].[private] = 1 AND (@HasViewUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                OR 
                ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [L].[private] = 1 AND (
                        @HasViewLawyerAccountUserPermission       = 1 
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
                ([U].[private] = 1 AND (@HasViewUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                OR 
                ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [C].[private] = 1 AND (
                        @HasViewCustomerAccountUserPermission       = 1 
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
                ([U].[private] = 1 AND (@HasViewUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                OR 
                ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [L].[private] = 1 AND (
                        @HasViewLawyerAccountUserPermission       = 1
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
                ([U].[private] = 1 AND (@HasViewUserPermission = 1 OR @HasViewAnyUserPermission = 1))
                OR 
                ([U].[private] = 0 AND (@HasViewPublicUserPermission = 1 OR @HasViewAnyUserPermission = 1))
            )
            AND
            (
                CASE
                    WHEN [C].[private] = 1 AND (
                        @HasViewCustomerAccountUserPermission       = 1 
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

        @HasViewUserPermission = 1

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

            return resultConstructor.Build<DetailsInformation>(information);
        });

        return result;
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

    #endregion

    #region EditAsync

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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @EditOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [EDIT_OWN_USER]

    (@EditOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @EditOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId        AND
            [PGR].[user_id]         = @UserId               AND
            [PGR].[permission_id]   = @EditUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @EditAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [EDIT_ANY_USER]

    (@EditAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @EditAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @EditAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [EDIT_ANY_LAWYER_ACCOUNT_USER]

    (@EditAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @EditAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @EditAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [EDIT_ANY_CUSTOMER_ACCOUNT_USER]

    (@EditAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @EditAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                     AND
            [PGR].[user_id]         = @UserId                            AND
            [PGR].[permission_id]   = @EditLawyerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                       AND
            [PGR].[user_id]         = @UserId                              AND
            [PGR].[permission_id]   = @EditCustomerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_USER]

    (@ViewAnyUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_LAWYER_ACCOUNT_USER]

    (@ViewAnyLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyLawyerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU].[user_id]       = @UserId                                 AND
            [PGU].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CUSTOMER_ACCOUNT_USER]

    (@ViewAnyCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        WHERE 
            [PG].[permission_id] = @ViewAnyCustomerAccountUserPermissionId AND
            [PG].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_USER]

    (@ViewPublicUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                  AND
            [PGU_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_LAWYER_ACCOUNT_USER]

    (@ViewPublicLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                    AND
            [PGU_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER]

    (@ViewPublicCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId        AND
            [PGR].[user_id]         = @UserId               AND
            [PGR].[permission_id]   = @ViewUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                     AND
            [PGR].[user_id]         = @UserId                            AND
            [PGR].[permission_id]   = @ViewLawyerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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
        WHERE 
            [PGR].[related_user_id] = @RelatedUserId                       AND
            [PGR].[user_id]         = @UserId                              AND
            [PGR].[permission_id]   = @ViewCustomerAccountUserPermissionId AND
            [PGR].[role_id]         = @RoleId
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