﻿using Dapper;
using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.Domain.Case.Interfaces.Services;
using LawyerCustomerApp.Domain.Case.Repositories.Models;
using LawyerCustomerApp.Domain.Case.Responses.Repositories.Error;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Text;

using PermissionSymbols = LawyerCustomerApp.External.Models.Permission.Permissions;

namespace LawyerCustomerApp.Domain.Case.Repositories;

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
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to USER or ROLE permission]

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewOwnCasePermission',    @ViewOwnCasePermissionId),
    ('HasViewPublicCasePermission', @ViewPublicCasePermissionId),
    ('HasViewAnyCasePermission',    @ViewAnyCasePermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new
            {
                UserId      = parameters.UserId,
                AttributeId = parameters.AttributeId,
                RoleId      = parameters.RoleId,

                ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
                ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,
                ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Search>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                UserId      = parameters.UserId,
                AttributeId = parameters.AttributeId,
                RoleId      = parameters.RoleId,

                ViewCasePermissionId = permission.ViewCasePermissionId,

                HasViewAnyCasePermission    = permissionsResult.HasViewAnyCasePermission,
                HasViewPublicCasePermission = permissionsResult.HasViewPublicCasePermission,
                HasViewOwnCasePermission    = permissionsResult.HasViewOwnCasePermission,

                Limit  = parameters.Pagination.End - parameters.Pagination.Begin + 1,
                Offset = parameters.Pagination.Begin - 1,

                TitleFilter = string.IsNullOrWhiteSpace(parameters.Query) ? null : $"%{parameters.Query}%"
            };

            const string queryText = $@"
SELECT
    [C].[id]          AS [Id],    
    [C].[user_id]     AS [UserId],
    [C].[title]       AS [Title],
    [C].[description] AS [Description],
    [C].[customer_id] AS [CustomerId],
    [C].[lawyer_id]   AS [LawyerId]
FROM [cases] [C]
WHERE

    -- [Basic non-permission filter]

    (@TitleFilter IS NULL OR [C].[title] LIKE @TitleFilter)
    AND (
       
        -- [Block 1: User has permission to view the case]

        ([C].[user_id] = @UserId AND @HasViewOwnCasePermission = 1)

        OR

        (@ViewCasePermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_case] [PGC]
            LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
            WHERE
                [PGC].[related_case_id] = [C].[id]              AND 
                [PGC].[user_id]         = @UserId               AND 
                [PGC].[permission_id]   = @ViewCasePermissionId AND 
                [PGC].[role_id]         = @RoleId               AND 
                ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
        ))
        OR

        @HasViewAnyCasePermission = 1

        OR

        -- [Block 2: Case is public and user has public view permission]

        ([C].[private] = 0 AND (
            @HasViewPublicCasePermission = 1
            OR @HasViewAnyCasePermission = 1
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
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to USER or ROLE permission]

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer)
        };

        var information = await ValuesExtensions.GetValue(async () =>
        {
            // [Permissions Queries]

            const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewOwnCasePermission',    @ViewOwnCasePermissionId),
    ('HasViewPublicCasePermission', @ViewPublicCasePermissionId),
    ('HasViewAnyCasePermission',    @ViewAnyCasePermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new
            {
                UserId      = parameters.UserId,
                AttributeId = parameters.AttributeId,
                RoleId      = parameters.RoleId,

                ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
                ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,
                ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId
            };

            var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Count>(queryPermissions, queryPermissionsParameters);

            // [Principal Query]

            var queryParameters = new
            {
                UserId      = parameters.UserId,
                AttributeId = parameters.AttributeId,
                RoleId      = parameters.RoleId,

                ViewCasePermissionId = permission.ViewCasePermissionId,

                HasViewAnyCasePermission    = permissionsResult.HasViewAnyCasePermission,
                HasViewPublicCasePermission = permissionsResult.HasViewPublicCasePermission,
                HasViewOwnCasePermission    = permissionsResult.HasViewOwnCasePermission,

                TitleFilter = string.IsNullOrWhiteSpace(parameters.Query) ? null : $"%{_hashService.Encrypt(parameters.Query)}%"
            };

            const string queryText = $@"
SELECT
    COUNT(*)
FROM [cases] [C]
WHERE

    -- [Basic non-permission filter]

    (@TitleFilter IS NULL OR [C].[title] LIKE @TitleFilter)
    AND (
       
        -- [Block 1: User has permission to view the case]

        ([C].[user_id] = @UserId AND @HasViewOwnCasePermission = 1)

        OR

        (@ViewCasePermissionId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [permission_grants_case] [PGC]
            LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
            WHERE
                [PGC].[related_case_id] = [C].[id]              AND 
                [PGC].[user_id]         = @UserId               AND 
                [PGC].[permission_id]   = @ViewCasePermissionId AND 
                [PGC].[role_id]         = @RoleId               AND 
                ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
        ))
        OR

        @HasViewAnyCasePermission = 1

        OR

        -- [Block 2: Case is public and user has public view permission]

        ([C].[private] = 0 AND (
            @HasViewPublicCasePermission = 1
            OR @HasViewAnyCasePermission = 1
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
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to USER or ROLE permission]

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer)
        };

        // [Permissions Queries]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasViewOwnCasePermission',    @ViewOwnCasePermissionId),
    ('HasViewPublicCasePermission', @ViewPublicCasePermissionId),
    ('HasViewAnyCasePermission',    @ViewAnyCasePermissionId),
    ('HasViewCasePermission',       @ViewCasePermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'       AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCasePermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

            var queryPermissionsParameters = new
            {
                UserId        = parameters.UserId,
                AttributeId   = parameters.AttributeId,
                RoleId        = parameters.RoleId,
                RelatedCaseId = parameters.CaseId,

                ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
                ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,
                ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
                ViewCasePermissionId       = permission.ViewCasePermissionId
            };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Details>(queryPermissions, queryPermissionsParameters);

        // [Principal Query]

        var queryParameters = new
        {
            UserId      = parameters.UserId,
            CaseId      = parameters.CaseId,
            AttributeId = parameters.AttributeId,
            RoleId      = parameters.RoleId,

            HasViewAnyCasePermission    = permissionsResult.HasViewAnyCasePermission,
            HasViewPublicCasePermission = permissionsResult.HasViewPublicCasePermission,
            HasViewOwnCasePermission    = permissionsResult.HasViewOwnCasePermission,
            HasViewCasePermission       = permissionsResult.HasViewCasePermission
        };

        const string queryText = $@"
SELECT
    [C].[id]          AS [Id],    
    [C].[user_id]     AS [UserId],
    [C].[title]       AS [Title],
    [C].[description] AS [Description],
    [C].[customer_id] AS [CustomerId],
    [C].[lawyer_id]   AS [LawyerId]
FROM [cases] [C]
WHERE 

    -- [Basic non-permission filter]

    [C].[id] = @CaseId
    AND (
       
        -- [Block 1: User has permission to view the case]

        ([C].[user_id] = @UserId AND (@HasViewOwnCasePermission = 1 OR @HasViewAnyCasePermission = 1))

        OR

        @HasViewCasePermission = 1

        OR

        @HasViewAnyCasePermission = 1

        OR

        -- [Block 2: Case is public and user has public view permission]

        ([C].[private] = 0 AND (
            @HasViewPublicCasePermission = 1
            OR @HasViewAnyCasePermission = 1
        ))
    );";

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
            resultConstructor.SetConstructor(new CaseNotFoundError());

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

            RegisterCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REGISTER_CASE, contextualizer)
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
    ('HasRegisterCasePermission', @RegisterCasePermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasRegisterCasePermission' THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasRegisterCasePermission]  
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            RegisterCasePermissionId = permission.RegisterCasePermissionId,

            UserId      = parameters.UserId,
            AttributeId = parameters.AttributeId,
            RoleId      = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Register>(queryPermissions, queryPermissionsParameters);
    
        if (!permissionsResult.HasRegisterCasePermission)
        {
            resultConstructor.SetConstructor(new RegisterCaseDeniedError());
    
            return resultConstructor.Build();
        }
    
        var actualDate = DateTime.Now;
    
        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new 
            {
                Title       = parameters.Title,
                Description = parameters.Description,
                Status      = "openned",
                BeginDate   = actualDate,
    
                UserId = parameters.UserId
            };
    
            var stringBuilder = new StringBuilder();
    
            stringBuilder.Append(@"INSERT INTO [cases] ([title], [description], [status], [begin_date], [user_id])
                                                VALUES (@Title, @Description, @Status, @BeginDate, @UserId)");
    
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
            resultConstructor.SetConstructor(new RegisterCaseInsertionError());
    
            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }

    #endregion

    #region AssignLawyerAsync

    public async Task<Result> AssignLawyerAsync(AssignLawyerParameters parameters, Contextualizer contextualizer)
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

            AssignLawyerCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_LAWYER_CASE, contextualizer),

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to USER or ROLE permission]

            AssignLawyerOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_LAWYER_OWN_CASE, contextualizer),

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            AssignLawyerAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_LAWYER_ANY_CASE, contextualizer),

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer)
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

        // [Case Id]
        var caseIdResult = await ValidateCaseId(
            parameters.CaseId,
            contextualizer);

        if (caseIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(caseIdResult);

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

    ('HasAssignLawyerCasePermission',       @AssignLawyerCasePermissionId),
    ('HasAssignOwnLawyerCasePermission',    @AssignOwnLawyerCasePermissionId),
    ('HasAssignAnyLawyerCasePermission',    @AssignAnyLawyerCasePermissionId),
    ('HasViewCasePermission',               @ViewCasePermissionId),
    ('HasViewOwnCasePermission',            @ViewOwnCasePermissionId),
    ('HasViewAnyCasePermission',            @ViewAnyCasePermissionId),
    ('HasViewPublicCasePermission',         @ViewPublicCasePermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignLawyerCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasAssignLawyerCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignOwnLawyerCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasAssignOwnLawyerCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignAnyLawyerCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasAssignAnyLawyerCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'            AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'         AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'         AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission'      AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            AssignLawyerCasePermissionId    = permission.AssignLawyerCasePermissionId,
            AssignLawyerOwnCasePermissionId = permission.AssignLawyerOwnCasePermissionId,
            AssignLawyerAnyCasePermissionId = permission.AssignLawyerAnyCasePermissionId,

            ViewCasePermissionId       = permission.ViewCasePermissionId,   
            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,

            UserId      = parameters.UserId,
            CaseId      = parameters.CaseId,
            AttributeId = parameters.AttributeId,
            RoleId      = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.AssignLawyer>(queryPermissions, queryPermissionsParameters);

        // [Case Information]

        const string queryCaseInformations = "SELECT [C].[Private], CASE WHEN [C].[user_id] = @UserId THEN 1 ELSE 0 END AS [Owner] FROM [cases] [C] WHERE [C].[id] = @CaseId";

        var queryCaseInformationParameters = new
        {
            UserId = parameters.UserId,
            CaseId = parameters.CaseId
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

        // [ASSIGN_LAWYER]
        if (((caseInformationResult.Owner.HasValue && caseInformationResult.Owner.Value) && !permissionsResult.HasAssignLawyerOwnCasePermission) &&
            !permissionsResult.HasAssignLawyerCasePermission &&
            !permissionsResult.HasAssignLawyerAnyCasePermission)
        {
            resultConstructor.SetConstructor(new AssignLawyerDeniedError());

            return resultConstructor.Build();
        }

        var updatedItems = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new 
            {
                CaseId   = parameters.CaseId,
                UserId   = parameters.UserId,
                LawyerId = parameters.LawyerId == 0 ? null : (int?)parameters.LawyerId
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"UPDATE [cases] C SET [C].[lawyer_id] = @LawyerId WHERE [C].[id] = @CaseId AND [C].[user_id] = @UserId");

            var includedItems = await connection.Connection.ExecuteAsync(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return includedItems;
        });

        if (updatedItems == 0)
        {
            resultConstructor.SetConstructor(new RegisterCaseInsertionError());

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }

    #endregion

    #region AssignCustomerAsync

    public async Task<Result> AssignCustomerAsync(AssignCustomerParameters parameters, Contextualizer contextualizer)
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

            AssignCustomerCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_CUSTOMER_CASE, contextualizer),

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to USER or ROLE permission]

            AssignCustomerOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_CUSTOMER_OWN_CASE, contextualizer),

            ViewOwnCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),
            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            AssignCustomerAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.ASSIGN_CUSTOMER_ANY_CASE, contextualizer),

            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer)
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

        // [Case Id]
        var caseIdResult = await ValidateCaseId(
            parameters.CaseId,
            contextualizer);

        if (caseIdResult.IsFinished)
            return resultConstructor.Build().Incorporate(caseIdResult);

        // [Attribute Account]
        var attributeAccountResult = await ValidateAttributeAccount(
            parameters.UserId,
            parameters.AttributeId,
            contextualizer);

        if (attributeAccountResult.IsFinished)
            return resultConstructor.Build().Incorporate(attributeAccountResult);

        // [Permissions Validation]

        const string queryPermissions = @"
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES

    ('HasAssignCustomerCasePermission',       @AssignCustomerCasePermissionId),
    ('HasAssignOwnCustomerCasePermission',    @AssignOwnCustomerCasePermissionId),
    ('HasAssignAnyCustomerCasePermission',    @AssignAnyCustomerCasePermissionId),
    ('HasViewCasePermission',               @ViewCasePermissionId),
    ('HasViewOwnCasePermission',            @ViewOwnCasePermissionId),
    ('HasViewAnyCasePermission',            @ViewAnyCasePermissionId),
    ('HasViewPublicCasePermission',         @ViewPublicCasePermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignCustomerCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasAssignCustomerCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignOwnCustomerCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasAssignOwnCustomerCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasAssignAnyCustomerCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasAssignAnyCustomerCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'              AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'           AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'           AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission'        AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            AssignCustomerCasePermissionId    = permission.AssignCustomerCasePermissionId,
            AssignCustomerOwnCasePermissionId = permission.AssignCustomerOwnCasePermissionId,
            AssignCustomerAnyCasePermissionId = permission.AssignCustomerAnyCasePermissionId,

            ViewCasePermissionId       = permission.ViewCasePermissionId,   
            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,

            UserId      = parameters.UserId,
            CaseId      = parameters.CaseId,
            AttributeId = parameters.AttributeId,
            RoleId      = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.AssignCustomer>(queryPermissions, queryPermissionsParameters);

        // [Case Information]

        const string queryCaseInformations = "SELECT [C].[Private], CASE WHEN [C].[user_id] = @UserId THEN 1 ELSE 0 END AS [Owner] FROM [cases] [C] WHERE [C].[id] = @CaseId";

        var queryCaseInformationParameters = new
        {
            UserId = parameters.UserId,
            CaseId = parameters.CaseId
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

        // [ASSIGN_CUSTOMER]
        if (((caseInformationResult.Owner.HasValue && caseInformationResult.Owner.Value) && !permissionsResult.HasAssignCustomerOwnCasePermission) &&
            !permissionsResult.HasAssignCustomerCasePermission &&
            !permissionsResult.HasAssignCustomerAnyCasePermission)
        {
            resultConstructor.SetConstructor(new AssignCustomerDeniedError());

            return resultConstructor.Build();
        }

        var updatedItems = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new 
            {
                CaseId     = parameters.CaseId,
                UserId     = parameters.UserId,
                CustomerId = parameters.CustomerId == 0 ? null : (int?)parameters.CustomerId
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"UPDATE [cases] C SET [C].[customer_id] = @CustomerId WHERE [C].[id] = @CaseId AND [C].[user_id] = @UserId");

            var includedItems = await connection.Connection.ExecuteAsync(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return includedItems;
        });

        if (updatedItems == 0)
        {
            resultConstructor.SetConstructor(new RegisterCaseInsertionError());

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

            EditCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_CASE, contextualizer),
           
            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),
            
            // [Related to USER or ROLE permission]

            EditOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_OWN_CASE, contextualizer),

            ViewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_PUBLIC_CASE, contextualizer),
         
            ViewOwnCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_OWN_CASE, contextualizer),

            // [Related to SUPER USER or ADMIN permission]

            EditAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.EDIT_ANY_CASE, contextualizer),
          
            ViewAnyCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer)
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
WITH [permission_checks]([permission_name], [permission_id]) AS (
    VALUES
    ('HasEditCasePermission',       @EditCasePermissionId),
    ('HasEditOwnCasePermission',    @EditOwnCasePermissionId),
    ('HasEditAnyCasePermission',    @EditAnyCasePermissionId),
    ('HasViewCasePermission',       @ViewCasePermissionId),
    ('HasViewOwnCasePermission',    @ViewOwnCasePermissionId),
    ('HasViewAnyCasePermission',    @ViewAnyCasePermissionId),
    ('HasViewPublicCasePermission', @ViewPublicCasePermissionId)
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
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditCaseCasePermission'   AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasEditCustomerCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditOwnCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasEditOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasEditAnyCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasEditAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewCasePermission'       AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewOwnCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewOwnCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewAnyCasePermission'    AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewAnyCasePermission],
    MAX(CASE WHEN [PC].[permission_name] = 'HasViewPublicCasePermission' AND ([G].[attribute_id] IS NULL OR [G].[attribute_id] = @AttributeId) THEN COALESCE([G].[granted],0) ELSE 0 END) AS [HasViewPublicCasePermission]
FROM [permission_checks] [PC]
LEFT JOIN [grants] [G] ON [G].[permission_name] = [PC].[permission_name];";

        var queryPermissionsParameters = new
        {
            EditCasePermissionId    = permission.EditCasePermissionId,
            EditOwnCasePermissionId = permission.EditOwnCasePermissionId,
            EditAnyCasePermissionId = permission.EditAnyCasePermissionId,

            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,               
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,               
            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewCasePermissionId       = permission.ViewCasePermissionId,
         
            RelatedCaseId = parameters.RelatedCaseId,
            UserId        = parameters.UserId,
            RoleId        = parameters.RoleId,
            AttributeId   = parameters.AttributeId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.Edit>(queryPermissions, queryPermissionsParameters);

        // [Case Information]

        const string queryCaseInformations = @"
SELECT 
    [C].[private]                                  AS [Private], 
    CASE WHEN [C].[id] = @UserId THEN 1 ELSE 0 END AS [Owner]
FROM [cases] [C] WHERE [C].[id] = @RelatedCaseId AND [C].[user_id] = @UserId";

        var queryCaseInformationParameters = new
        {
            RelatedCaseId = parameters.RelatedCaseId,
            UserId        = parameters.UserId
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

        // [EDIT]
        if (((caseInformationResult.Owner.HasValue && caseInformationResult.Owner.Value) && !permissionsResult.HasEditOwnCasePermission) &&
            !permissionsResult.HasEditCasePermission &&
            !permissionsResult.HasEditAnyCasePermission)
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

                // =================== [Table - cases] =================== //

                if (parameters.HasChanges)
                {
                    queryParameters.Add("@CaseId", parameters.RelatedCaseId);
                    queryParameters.Add("@UserId", parameters.UserId);

                    // [Title]
                    if (parameters.Title.Received)
                    {
                        dynamicSetStattement.Add("[title] = @Title");
                        queryParameters.Add("@Title", parameters.Title.Value);
                    }

                    // [Description]
                    if (parameters.Description.Received)
                    {
                        dynamicSetStattement.Add("[description] = @Description");
                        queryParameters.Add("@Description", parameters.Description.Value);
                    }

                    // [Status]
                    if (parameters.Status.Received)
                    {
                        dynamicSetStattement.Add("[status] = @Status");
                        queryParameters.Add("@Status", parameters.Status.Value);
                    }

                    // [Private]
                    if (parameters.Private.Received)
                    {
                        dynamicSetStattement.Add("[private] = @Private");
                        queryParameters.Add("@Private", parameters.Private.Value);
                    }

                    if (dynamicSetStattement.Any())
                    {
                        var query = $"UPDATE [cases] SET {string.Join(",", dynamicSetStattement)} WHERE [id] = @CaseId AND [user_id] = @UserId";

                        var includedItems = await connection.Connection.ExecuteAsync(
                            new CommandDefinition(
                                    commandText:       query,
                                    parameters:        queryParameters,
                                    transaction:       connection.Transaction,
                                    cancellationToken: contextualizer.CancellationToken,
                                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
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