using Dapper;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Customer.Common.Models;
using LawyerCustomerApp.Domain.Customer.Interfaces.Services;
using LawyerCustomerApp.Domain.Customer.Repositories.Models;
using LawyerCustomerApp.Domain.Customer.Responses.Repositories.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System.Text;

using PermissionSymbols = LawyerCustomerApp.External.Models.Permission.Permissions;

namespace LawyerCustomerApp.Domain.Customer.Repositories;

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
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewOwnUserPermission',                   @ViewOwnUserPermissionId),
    ('HasViewOwnCustomerAccountUserPermission',    @ViewOwnCustomerAccountUserPermissionId),
    ('HasViewPublicUserPermission',                @ViewPublicUserPermissionId),
    ('HasViewPublicCustomerAccountUserPermission', @ViewPublicCustomerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                   @ViewAnyUserPermissionId),
    ('HasViewAnyCustomerAccountUserPermission',    @ViewAnyCustomerAccountUserPermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCustomerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,               
                ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

                ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
                ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

                ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
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
                HasViewOwnCustomerAccountUserPermission = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                
                HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                HasViewAnyCustomerAccountUserPermission = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                
                HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,
                
                // [ACL]
                
                ViewUserPermissionId                = permission.ViewUserPermissionId,
                ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                 
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
    [C].[id] AS [CustomerId],
    [U].[name]
FROM [users] [U]
RIGHT JOIN [customers] [C] ON [C].[user_id] = [U].[id]
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
        
        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_CUSTOMER_ACCOUNT_USER | VIEW_CUSTOMER_ACCOUNT_USER]

        ([C].[user_id] = @UserId AND @HasViewOwnCustomerAccountUserPermission = 1)

        OR

        (@ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            LEFT JOIN [attributes] [A_PGR] ON [A_PGR].[id] = [PGR].[attribute_id]
            WHERE
                [PGR].[related_user_id] = [U].[id]                             AND
                [PGR].[user_id]         = @UserId                              AND
                [PGR].[permission_id]   = @ViewCustomerAccountUserPermissionId AND
                [PGR].[role_id]         = @RoleId                              AND
                ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
        ))

        OR

        @HasViewAnyCustomerAccountUserPermission = 1

        OR

        -- [Block 2: Customer Account is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicCustomerAccountUserPermission = 1
            OR @HasViewAnyCustomerAccountUserPermission = 1
        ))

    )

ORDER BY [C].[id] DESC
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
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewOwnUserPermission',                   @ViewOwnUserPermissionId),
    ('HasViewOwnCustomerAccountUserPermission',    @ViewOwnCustomerAccountUserPermissionId),
    ('HasViewPublicUserPermission',                @ViewPublicUserPermissionId),
    ('HasViewPublicCustomerAccountUserPermission', @ViewPublicCustomerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                   @ViewAnyUserPermissionId),
    ('HasViewAnyCustomerAccountUserPermission',    @ViewAnyCustomerAccountUserPermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCustomerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new 
            { 
                ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,               
                ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

                ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
                ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

                ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
                ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

                AttributeId = parameters.UserId,
                UserId      = parameters.UserId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Count>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                 // [NOT ACL]

                 HasViewOwnUserPermission                = permissionsResult.HasViewOwnUserPermission,
                 HasViewOwnCustomerAccountUserPermission = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                 
                 HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyCustomerAccountUserPermission = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                 
                 HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,
                 
                 // [ACL]
                 
                 ViewUserPermissionId                = permission.ViewUserPermissionId,
                 ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,
                                                  
                 AttributeId  = parameters.AttributeId,
                 UserId       = parameters.UserId,
                 RoleId       = parameters.RoleId,

                NameFilter = string.IsNullOrWhiteSpace(parameters.Query) ? null : $"%{_hashService.Encrypt(parameters.Query)}%"
            };

            var queryText = $@"
SELECT
    COUNT(*)
FROM [users] [U]
RIGHT JOIN [customers] [C] ON [C].[user_id] = [U].[id]
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
        
        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_CUSTOMER_ACCOUNT_USER | VIEW_CUSTOMER_ACCOUNT_USER]

        ([C].[user_id] = @UserId AND @HasViewOwnCustomerAccountUserPermission = 1)

        OR

        (@ViewCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_relationship] [PGR]
            LEFT JOIN [attributes] [A_PGR] ON [A_PGR].[id] = [PGR].[attribute_id]
            WHERE
                [PGR].[related_user_id] = [U].[id]                             AND
                [PGR].[user_id]         = @UserId                              AND
                [PGR].[permission_id]   = @ViewCustomerAccountUserPermissionId AND
                [PGR].[role_id]         = @RoleId                              AND
                ([PGR].[attribute_id] IS NULL OR [A_PGR].[id] = @AttributeId)
        ))

        OR

        @HasViewAnyCustomerAccountUserPermission = 1

        OR

        -- [Block 2: Customer Account is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicCustomerAccountUserPermission = 1
            OR @HasViewAnyCustomerAccountUserPermission = 1
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
            
            ViewUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

            // [Related to USER or ROLE permission]

            ViewPublicUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_USER, contextualizer),
            ViewPublicCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CUSTOMER_ACCOUNT_USER, contextualizer),
            
            ViewOwnUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_USER, contextualizer),
            ViewOwnCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CUSTOMER_ACCOUNT_USER, contextualizer), 

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_USER, contextualizer),
            ViewAnyCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CUSTOMER_ACCOUNT_USER, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            DetailsInformation information;

            // [Check Permission Objects Permissions]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewCustomerAccountUserPermission',       @ViewCustomerAccountUserPermissionId),
    ('HasViewUserPermission',                      @ViewUserPermissionId),
    ('HasViewOwnUserPermission',                   @ViewOwnUserPermissionId),
    ('HasViewOwnCustomerAccountUserPermission',    @ViewOwnCustomerAccountUserPermissionId),
    ('HasViewPublicUserPermission',                @ViewPublicUserPermissionId),
    ('HasViewPublicCustomerAccountUserPermission', @ViewPublicCustomerAccountUserPermissionId),
    ('HasViewAnyUserPermission',                   @ViewAnyUserPermissionId),
    ('HasViewAnyCustomerAccountUserPermission',    @ViewAnyCustomerAccountUserPermissionId)
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
         [PGR].[related_user_id]   = (SELECT [U].[id] FROM [Users] [U] LEFT JOIN [customers] [C] ON [U].[id] = [C].[user_id] WHERE [C].[id] = @CustomerId)
)
SELECT
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCustomerAccountUserPermission'       AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewUserPermission'                      AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnUserPermission'                   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCustomerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicUserPermission'                AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCustomerAccountUserPermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCustomerAccountUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyUserPermission'                   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyUserPermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCustomerAccountUserPermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCustomerAccountUserPermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new 
            {
                ViewUserPermissionId                = permission.ViewUserPermissionId,
                ViewCustomerAccountUserPermissionId = permission.ViewCustomerAccountUserPermissionId,

                ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,               
                ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

                ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
                ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

                ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
                ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

                CustomerId  = parameters.CustomerId,
                AttributeId = parameters.UserId,
                UserId      = parameters.UserId,
                RoleId      = parameters.RoleId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Details>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                 // [NOT ACL]

                 HasViewOwnUserPermission                = permissionsResult.HasViewOwnUserPermission,
                 HasViewOwnCustomerAccountUserPermission = permissionsResult.HasViewOwnCustomerAccountUserPermission,
                 
                 HasViewAnyUserPermission                = permissionsResult.HasViewAnyUserPermission,
                 HasViewAnyCustomerAccountUserPermission = permissionsResult.HasViewAnyCustomerAccountUserPermission,
                 
                 HasViewPublicUserPermission                = permissionsResult.HasViewPublicUserPermission,
                 HasViewPublicCustomerAccountUserPermission = permissionsResult.HasViewPublicCustomerAccountUserPermission,
                 
                 // [ACL]
                 
                 HasViewUserPermission                = permissionsResult.HasViewUserPermission,
                 HasViewCustomerAccountUserPermission = permissionsResult.HasViewCustomerAccountUserPermission,

                 AttributeId  = parameters.AttributeId,
                 UserId       = parameters.UserId,
                 CustomerId   = parameters.CustomerId,
                 RoleId       = parameters.RoleId
            };

            var queryText = $@"
SELECT
    [U].[id] AS [UserId],
    [C].[id] AS [CustomerId],
    [U].[name]
FROM [users] [U]
RIGHT JOIN [customers] [C] ON [C].[user_id] = [U].[id]
WHERE
    [U].[id] = @UserId AND [C].[id] = @CustomerId
    AND (

        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_USER | VIEW_USER]

        ([U].[id] = @UserId AND (
            @HasViewOwnUserPermission    = 1 
            OR @HasViewAnyUserPermission = 1
        ))

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
        
        -- [Block 1: Has Specific or Global Grant for VIEW_ANY_CUSTOMER_ACCOUNT_USER | VIEW_CUSTOMER_ACCOUNT_USER]

        ([C].[user_id] = @UserId AND (
            @HasViewOwnCustomerAccountUserPermission    = 1
            OR @HasViewAnyCustomerAccountUserPermission = 1
        ))

        OR

        (@HasViewCustomerAccountUserPermission = 1 OR @HasViewAnyCustomerAccountUserPermission = 1)

        OR

        @HasViewAnyCustomerAccountUserPermission = 1

        OR

        -- [Block 2: Customer Account is Public AND User Has Public View Grant]

        ([U].[private] = 0 AND (
            @HasViewPublicCustomerAccountUserPermission = 1
            OR @HasViewAnyCustomerAccountUserPermission = 1
        ))
);";

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

    #endregion

    #region RegisterAsync

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

            RegisterCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REGISTER_CUSTOMER_ACCOUNT_USER, contextualizer)
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
    ('HasRegisterCustomerAccountUserPermission', @RegisterCustomerAccountUserPermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasRegisterCustomerAccountUserPermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRegisterCustomerAccountUserPermission]  
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            RegisterCustomerAccountUserPermissionId = permission.RegisterCustomerAccountUserPermissionId,

            UserId = parameters.UserId,
            RoleId = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Register>(queryPermissions, queryPermissionsParameters);


        if (!permissionsResult.HasRegisterCustomerAccountUserPermission)
        {
            resultConstructor.SetConstructor(new RegisterDeniedError());
    
            return resultConstructor.Build();
        }

        var userAlreadyHaveCustomerAccount = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedPhone = _hashService.Encrypt(parameters.Phone);

            var queryParameters = new
            {
                UserId  = parameters.UserId,
                Phone   = encrpytedPhone
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT CASE WHEN EXISTS (SELECT 1 FROM [customers] C WHERE [C].[user_id] = @UserId) 
                                        THEN 1 ELSE 0 
                                   END AS [user_already_have_customer_account]");

            var userAlreadyHaveCustomerAccount = await connection.Connection.QueryFirstAsync<bool>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return userAlreadyHaveCustomerAccount;
        });

        if (userAlreadyHaveCustomerAccount)
        {
            resultConstructor.SetConstructor(
                new UserAlreadyHaveCustomerAccountError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedPhone = _hashService.Encrypt(parameters.Phone);

            var queryParameters = new 
            {
                UserId = parameters.UserId,
                Phone  = encrpytedPhone
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("INSERT INTO [customers] ([user_id], [phone]) VALUES (@UserId, @Phone)");

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