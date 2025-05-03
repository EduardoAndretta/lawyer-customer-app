using Dapper;
using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.Domain.Case.Interfaces.Services;
using LawyerCustomerApp.Domain.Case.Repositories.Models;
using LawyerCustomerApp.Domain.Case.Responses.Repositories.Error;
using LawyerCustomerApp.Domain.Case.Responses.Repositories.Success;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
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
SELECT

/* ---------------------------------------------- [VIEW_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A] ON [PG].[attribute_id] = [A].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCasePermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                     AND
            [PGU_PUB].[permission_id] = @ViewPublicCasePermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                     AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewPublicCasePermissionId AND
            [PG_PUB].[role_id]       = @RoleId                     AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCasePermission],

/* ---------------------------------------------- [VIEW_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCasePermission]";

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

                TitleFilter = string.IsNullOrWhiteSpace(parameters.Query) ? null : $"%{parameters.Query}%",

                Limit  = parameters.Pagination.End - parameters.Pagination.Begin + 1,
                Offset = parameters.Pagination.Begin - 1
            };

            const string queryText = $@"
SELECT
    [C].[id],
    [C].[title],
    [C].[status],
    [C].[begin_date] AS [BeginDate]
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
                [PGC].[related_case_id] = [C].[id]
                AND [PGC].[user_id] = @UserId
                AND [PGC].[permission_id] = @ViewCasePermissionId
                AND [PGC].[role_id] = @RoleId
                AND ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
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

            var information = new SearchInformation
            {
                Items = new List<SearchInformation.ItemProperties>(),
            };

            using (var multiple = await connection.Connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds
                    )))
            {
                information = information with
                {
                    Items = await multiple.ReadAsync<SearchInformation.ItemProperties>()
                };
            }

            return information;
        });

        return resultConstructor.Build<SearchInformation>(information);
    }

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
SELECT

/* ---------------------------------------------- [REGISTER_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [REGISTER_CASE]

    (@RegisterCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                   AND
            [PGU].[permission_id] = @RegisterCasePermissionId AND
            [PGU].[role_id]       = @RoleId                   AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REGISTER_CASE]

    (@RegisterCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @RegisterCasePermissionId AND
            [PG].[role_id]       = @RoleId                   AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRegisterCasePermission]";

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
    
                UserId     = parameters.UserId,
                CustomerId = parameters.CustomerId == 0 ? null : (int?)parameters.CustomerId,
                LawyerId   = parameters.LawyerId   == 0 ? null : (int?)parameters.LawyerId
            };
    
            var stringBuilder = new StringBuilder();
    
            stringBuilder.Append(@"INSERT INTO [cases] ([title], [description], [status], [begin_date], [user_id], [customer_id], [lawyer_id])
                                                VALUES (@Title, @Description, @Status, @BeginDate, @UserId, @CustomerId, @LawyerId)");
    
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
SELECT

/* ---------------------------------------------- [ASSIGN_LAWYER_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [ASSIGN_LAWYER_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                          AND
            [PGU].[permission_id] = @AssignLawyerOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                          AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [ASSIGN_LAWYER_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @AssignLawyerOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                          AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasAssignLawyerOwnCasePermission],

/* ---------------------------------------------- [ASSIGN_LAWYER_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [ASSIGN_LAWYER_CASE]

    (@AssignLawyerCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId                       AND
            [PGC].[user_id]         = @UserId                       AND
            [PGC].[permission_id]   = @AssignLawyerCasePermissionId AND
            [PGC].[role_id]         = @RoleId                       AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasAssignLawyerCasePermission],

/* ---------------------------------------------- [ASSIGN_LAWYER_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [ASSIGN_LAWYER_ANY_CASE]

    (@AssignLawyerAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                          AND
            [PGU].[permission_id] = @AssignLawyerAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                          AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [ASSIGN_LAWYER_ANY_CASE]

    (@AssignLawyerAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @AssignLawyerAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                          AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasAssignLawyerAnyCasePermission],


/* ---------------------------------------------- [VIEW_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCasePermission],

/* ---------------------------------------------- [VIEW_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [VIEW_CASE]

    (@ViewCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId               AND
            [PGC].[user_id]         = @UserId               AND
            [PGC].[permission_id]   = @ViewCasePermissionId AND
            [PGC].[role_id]         = @RoleId               AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewCasePermission],

/* ---------------------------------------------- [VIEW_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCasePermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                     AND
            [PGU].[permission_id] = @ViewPublicCasePermissionId AND
            [PGU].[role_id]       = @RoleId                     AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewPublicCasePermissionId AND
            [PG].[role_id]       = @RoleId                     AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCasePermission]";

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
SELECT

/* ---------------------------------------------- [ASSIGN_CUSTOMER_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [ASSIGN_CUSTOMER_OWN_CASE]

    (@AssignCustomerOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                            AND
            [PGU].[permission_id] = @AssignCustomerOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                            AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [ASSIGN_CUSTOMER_OWN_CASE]

    (@AssignCustomerOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @AssignCustomerOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                            AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasAssignLawyerOwnCasePermission],

/* ---------------------------------------------- [ASSIGN_CUSTOMER_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [ASSIGN_CUSTOMER_CASE]

    (@AssignCustomerCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId                         AND
            [PGC].[user_id]         = @UserId                         AND
            [PGC].[permission_id]   = @AssignCustomerCasePermissionId AND
            [PGC].[role_id]         = @RoleId                         AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasAssignLawyerCasePermission],

/* ---------------------------------------------- [ASSIGN_CUSTOMER_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [ASSIGN_CUSTOMER_ANY_CASE]

    (@AssignCustomerAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                            AND
            [PGU].[permission_id] = @AssignCustomerAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                            AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [ASSIGN_CUSTOMER_ANY_CASE]

    (@AssignCustomerAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @AssignCustomerAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                            AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasAssignLawyerAnyCasePermission],

/* ---------------------------------------------- [VIEW_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCasePermission],

/* ---------------------------------------------- [VIEW_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [VIEW_CASE]

    (@ViewCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId               AND
            [PGC].[user_id]         = @UserId               AND
            [PGC].[permission_id]   = @ViewCasePermissionId AND
            [PGC].[role_id]         = @RoleId               AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewCasePermission],

/* ---------------------------------------------- [VIEW_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCasePermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                     AND
            [PGU].[permission_id] = @ViewPublicCasePermissionId AND
            [PGU].[role_id]       = @RoleId                     AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewPublicCasePermissionId AND
            [PG].[role_id]       = @RoleId                     AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCasePermission]";

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
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            GrantPermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CASE, contextualizer),

            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            GrantPermissionsUserPermissionId                = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_USER, contextualizer),
            GrantPermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            GrantPermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.GRANT_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),
           
            ViewUserPermissionId                           = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId            = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

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
SELECT

/* ---------------------------------------------- [GRANT_PERMISSIONS_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [GRANT_PERMISSIONS_OWN_CASE]

    (@GrantPermissionsOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                              AND
            [PGU].[permission_id] = @GrantPermissionsOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                              AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [GRANT_PERMISSIONS_OWN_CASE]

    (@GrantPermissionsOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                              AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsOwnCasePermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [GRANT_PERMISSIONS_CASE]

    (@GrantPermissionsCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId                           AND
            [PGC].[user_id]         = @UserId                           AND
            [PGC].[permission_id]   = @GrantPermissionsCasePermissionId AND
            [PGC].[role_id]         = @RoleId                           AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionCasePermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [GRANT_PERMISSIONS_ANY_CASE]

    (@GrantPermissionsAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                              AND
            [PGU].[permission_id] = @GrantPermissionsAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                              AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [GRANT_PERMISSIONS_ANY_CASE]

    (@GrantPermissionsAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @GrantPermissionsAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                              AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionsAnyCasePermission],

/* ---------------------------------------------- [VIEW_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCasePermission],

/* ---------------------------------------------- [VIEW_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [VIEW_CASE]

    (@ViewCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId               AND
            [PGC].[user_id]         = @UserId               AND
            [PGC].[permission_id]   = @ViewCasePermissionId AND
            [PGC].[role_id]         = @RoleId               AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewCasePermission],

/* ---------------------------------------------- [VIEW_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCasePermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                     AND
            [PGU].[permission_id] = @ViewPublicCasePermissionId AND
            [PGU].[role_id]       = @RoleId                     AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewPublicCasePermissionId AND
            [PG].[role_id]       = @RoleId                     AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCasePermission]";

        var queryPermissionsParameters = new
        {
            GrantPermissionsCasePermissionId    = permission.GrantPermissionsCasePermissionId,
            GrantPermissionsOwnCasePermissionId = permission.GrantPermissionsOwnCasePermissionId,
            GrantPermissionsAnyCasePermissionId = permission.GrantPermissionsAnyCasePermissionId,

            ViewCasePermissionId       = permission.ViewCasePermissionId,   
            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,

            UserId      = parameters.UserId,
            CaseId      = parameters.CaseId,
            AttributeId = parameters.AttributeId,
            RoleId      = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.GrantPermissions>(queryPermissions, queryPermissionsParameters);

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

        // [GRANT_PERMISSIONS]
        if (((caseInformationResult.Owner.HasValue && caseInformationResult.Owner.Value) && !permissionsResult.HasGrantPermissionsOwnCasePermission) &&
            !permissionsResult.HasGrantPermissionsCasePermission &&
            !permissionsResult.HasGrantPermissionsAnyCasePermission)
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
SELECT

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
END AS [HasGrantPermissionAnyUserPermission],

/* ---------------------------------------------- [GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [GRANT_PERMISSIONS_ANY_LAWYER_ACCOUNT_USER]

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
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasGrantPermissionOwnUserPermission],

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

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
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

/* ---------------------------------------------- [VIEW_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                  AND
            [PGU_PUB].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                  AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewOwnUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                  AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicUserPermission],

/* ---------------------------------------------- [VIEW_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                               AND
            [PGU_PUB].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                               AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                               AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_OWN_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                 AND
            [PGU_PUB].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                 AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                 AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCustomerAccountUserPermission]";

        var queryPermissionsParametersSpecificUser = new 
        { 
            ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
            ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,            
            ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

            GrantPermissionsAnyUserPermissionId                = permission.GrantPermissionsAnyUserPermissionId,
            GrantPermissionsAnyLawyerAccountUserPermissionId   = permission.GrantPermissionsAnyLawyerAccountUserPermissionId,
            GrantPermissionsAnyCustomerAccountUserPermissionId = permission.GrantPermissionsAnyCustomerAccountUserPermissionId,
            
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
(VIEW_CASE, ASSIGN_LAWYER_CASE, ASSIGN_CUSTOMER_CASE, ASSIGN_CUSTOMER_CASE, GRANT_PERMISSIONS_CASE, REVOKE_PERMISSIONS_CASE)";

        var allowedPermissions = await connection.Connection.QueryAsync<int?>(queryAllowedPermissions);

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

                    HasGrantPermissionAnyUserPermission                = permissionsResultSpecificUser.HasGrantPermissionsAnyUserPermission,
                    HasGrantPermissionAnyLawyerAccountUserPermission   = permissionsResultSpecificUser.HasGrantPermissionsAnyLawyerAccountUserPermission,
                    HasGrantPermissionAnyCustomerAccountUserPermission = permissionsResultSpecificUser.HasGrantPermissionsAnyCustomerAccountUserPermission,

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
                        @ViewUserPermissionId IS NOT NULL EXISTS (
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
                RelatedCaseId = parameters.CaseId,

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
            // [Related to CASE WITH (USER OR ROLE) specific permission assigned]

            RevokePermissionsCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CASE, contextualizer),
            
            ViewCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_CASE, contextualizer),

            // [Related to RELATIONSHIP WITH (USER OR ROLE) specific permission assigned]

            RevokePermissionsUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_USER, contextualizer),
            RevokePermissionsLawyerAccountUserPermissionId   = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_LAWYER_ACCOUNT_USER, contextualizer),
            RevokePermissionsCustomerAccountUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_CUSTOMER_ACCOUNT_USER, contextualizer),
           
            ViewUserPermissionId                           = await GetPermissionIdAsync(PermissionSymbols.VIEW_USER, contextualizer),
            ViewLawyerAccountUserPermissionId              = await GetPermissionIdAsync(PermissionSymbols.VIEW_LAWYER_ACCOUNT_USER, contextualizer),
            ViewCustomerAccountUserPermissionId            = await GetPermissionIdAsync(PermissionSymbols.VIEW_CUSTOMER_ACCOUNT_USER, contextualizer),

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

            RevokePermissionsAnyUserPermissionId = await GetPermissionIdAsync(PermissionSymbols.REVOKE_PERMISSIONS_ANY_USER, contextualizer),
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
SELECT

/* ---------------------------------------------- [REVOKE_PERMISSIONS_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [REVOKE_PERMISSIONS_OWN_CASE]

    (@RevokePermissionsOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @RevokePermissionsOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [REVOKE_PERMISSIONS_OWN_CASE]

    (@RevokePermissionsOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsOwnCasePermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [REVOKE_PERMISSIONS_CASE]

    (@RevokePermissionsCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId                            AND
            [PGC].[user_id]         = @UserId                            AND
            [PGC].[permission_id]   = @RevokePermissionsCasePermissionId AND
            [PGC].[role_id]         = @RoleId                            AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsCasePermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [REVOKE_PERMISSIONS_ANY_CASE]

    (@RevokePermissionsAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                               AND
            [PGU].[permission_id] = @RevokePermissionsAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                               AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_ANY_CASE]

    (@RevokePermissionsAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @RevokePermissionsAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                               AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasRevokePermissionsAnyCasePermission],


/* ---------------------------------------------- [VIEW_OWN_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: Ownership (User Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewOwnCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: Ownership (Role Grant)] [VIEW_OWN_CASE]

    (@ViewOwnCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewOwnCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCasePermission],

/* ---------------------------------------------- [VIEW_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_case (ACL Grant)] [VIEW_CASE]

    (@ViewCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_case] [PGC]
        LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
        WHERE 
            [PGC].[related_case_id] = @CaseId               AND
            [PGC].[user_id]         = @UserId               AND
            [PGC].[permission_id]   = @ViewCasePermissionId AND
            [PGC].[role_id]         = @RoleId               AND
            ([PGC].[attribute_id] IS NULL OR [A_PGC].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewCasePermission],

/* ---------------------------------------------- [VIEW_ANY_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                  AND
            [PGU].[permission_id] = @ViewAnyCasePermissionId AND
            [PGU].[role_id]       = @RoleId                  AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_ANY_CASE]

    (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewAnyCasePermissionId AND
            [PG].[role_id]       = @RoleId                  AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewAnyCasePermission],

/* ---------------------------------------------- [VIEW_PUBLIC_CASE] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                     AND
            [PGU].[permission_id] = @ViewPublicCasePermissionId AND
            [PGU].[role_id]       = @RoleId                     AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 1: permission_grants (Role Grant)] [VIEW_PUBLIC_CASE]

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG]
        LEFT JOIN [attributes] [A_PG] ON [PG].[attribute_id] = [A_PG].[id]
        WHERE 
            [PG].[permission_id] = @ViewPublicCasePermissionId AND
            [PG].[role_id]       = @RoleId                     AND
            ([PG].[attribute_id] IS NULL OR [A_PG].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicCasePermission]";

        var queryPermissionsParameters = new
        {
            RevokePermissionsCasePermissionId    = permission.RevokePermissionsCasePermissionId,
            RevokePermissionsOwnCasePermissionId = permission.RevokePermissionsOwnCasePermissionId,
            RevokePermissionsAnyCasePermissionId = permission.RevokePermissionsAnyCasePermissionId,

            ViewCasePermissionId       = permission.ViewCasePermissionId,   
            ViewOwnCasePermissionId    = permission.ViewOwnCasePermissionId,
            ViewAnyCasePermissionId    = permission.ViewAnyCasePermissionId,
            ViewPublicCasePermissionId = permission.ViewPublicCasePermissionId,

            UserId      = parameters.UserId,
            CaseId      = parameters.CaseId,
            AttributeId = parameters.AttributeId,
            RoleId      = parameters.RoleId
        };

        var permissionsResult = await connection.Connection.QueryFirstAsync<PermissionResult.RevokePermissions>(queryPermissions, queryPermissionsParameters);

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

        // [REVOKE_PERMISSIONS]
        if (((caseInformationResult.Owner.HasValue && caseInformationResult.Owner.Value) && !permissionsResult.HasRevokePermissionsOwnCasePermission) &&
            !permissionsResult.HasRevokePermissionsCasePermission &&
            !permissionsResult.HasRevokePermissionsAnyCasePermission)
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
END AS [HasRevokePermissionAnyUserPermission],

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
END AS [HasRevokePermissionOwnUserPermission],

/* ---------------------------------------------- [REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user  (User Grant)] [REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER]

    (@RevokePermissionsOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU]
        LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
        WHERE 
            [PGU].[user_id]       = @UserId                                            AND
            [PGU].[permission_id] = @RevokePermissionsOwnLawyerAccountUserPermissionId AND
            [PGU].[role_id]       = @RoleId                                            AND
            ([PGU].[attribute_id] IS NULL OR [A_PGU].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [REVOKE_PERMISSIONS_OWN_LAWYER_ACCOUNT_USER]

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

    (@ViewPublicCasePermissionId IS NOT NULL AND EXISTS (
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

/* ---------------------------------------------- [VIEW_OWN_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                  AND
            [PGU_PUB].[permission_id] = @ViewOwnUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                  AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_USER]

    (@ViewOwnUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewOwnUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                  AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicUserPermission],

/* ---------------------------------------------- [VIEW_OWN_LAWYER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                               AND
            [PGU_PUB].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                               AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_LAWYER_ACCOUNT_USER]

    (@ViewOwnLawyerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewOwnLawyerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                               AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewPublicLawyerAccountUserPermission],

/* ---------------------------------------------- [VIEW_OWN_CUSTOMER_ACCOUNT_USER] ---------------------------------------------- */

SELECT 
CASE 
    WHEN 

    -- [Layer 1: permission_grants_user (User Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants_user] [PGU_PUB]
        LEFT JOIN [attributes] [A_PGU_PUB] ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
        WHERE 
            [PGU_PUB].[user_id]       = @UserId                                 AND
            [PGU_PUB].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PGU_PUB].[role_id]       = @RoleId                                 AND
            ([PGU_PUB].[attribute_id] IS NULL OR [A_PGU_PUB].[id] = @AttributeId)
    )) OR 

    -- [Layer 2: permission_grants (Role Grant)] [VIEW_OWN_CUSTOMER_ACCOUNT_USER]

    (@ViewOwnCustomerAccountUserPermissionId IS NOT NULL AND EXISTS (
        SELECT 1 FROM [permission_grants] [PG_PUB]
        LEFT JOIN [attributes] [A_PG_PUB] ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
        WHERE 
            [PG_PUB].[permission_id] = @ViewOwnCustomerAccountUserPermissionId AND
            [PG_PUB].[role_id]       = @RoleId                                 AND
            ([PG_PUB].[attribute_id] IS NULL OR [A_PG_PUB].[id] = @AttributeId)
    )) THEN 1
    ELSE 0
END AS [HasViewOwnCustomerAccountUserPermission]";

        var queryPermissionsParametersSpecificUser = new 
        { 
            RevokePermissionsAnyUserPermissionId                = permission.RevokePermissionsAnyUserPermissionId,
            RevokePermissionsAnyLawyerAccountUserPermissionId   = permission.RevokePermissionsAnyLawyerAccountUserPermissionId,
            RevokePermissionsAnyCustomerAccountUserPermissionId = permission.RevokePermissionsAnyCustomerAccountUserPermissionId,
            
            RevokePermissionsOwnUserPermissionId                = permission.RevokePermissionsOwnUserPermissionId,
            RevokePermissionsOwnLawyerAccountUserPermissionId   = permission.RevokePermissionsOwnLawyerAccountUserPermissionId,
            RevokePermissionsOwnCustomerAccountUserPermissionId = permission.RevokePermissionsOwnCustomerAccountUserPermissionId,

            ViewPublicUserPermissionId                = permission.ViewPublicUserPermissionId,               
            ViewPublicLawyerAccountUserPermissionId   = permission.ViewPublicLawyerAccountUserPermissionId,            
            ViewPublicCustomerAccountUserPermissionId = permission.ViewPublicCustomerAccountUserPermissionId,

            ViewAnyUserPermissionId                = permission.ViewAnyUserPermissionId,
            ViewAnyLawyerAccountUserPermissionId   = permission.ViewAnyLawyerAccountUserPermissionId,
            ViewAnyCustomerAccountUserPermissionId = permission.ViewAnyCustomerAccountUserPermissionId,

            ViewOwnUserPermissionId                = permission.ViewOwnUserPermissionId,
            ViewOwnLawyerAccountUserPermissionId   = permission.ViewOwnLawyerAccountUserPermissionId,
            ViewOwnCustomerAccountUserPermissionId = permission.ViewOwnCustomerAccountUserPermissionId,

            AttributeId = parameters.UserId,
            UserId      = parameters.UserId,
            RoleId      = parameters.RoleId
        };

        var permissionsResultSpecificUser = await connection.Connection.QueryFirstAsync<PermissionResult.RevokePermissions.SpecificUser>(queryPermissionsSpecificUser, queryPermissionsParametersSpecificUser);

        const string queryAttributes = "SELECT [A].[id] AS [Id], [A].[name] AS [Name] FROM [Attributes]";

        var attributes = await connection.Connection.QueryAsync<(int Id, string Name)>(queryAttributes);

        const string queryAllowedPermissions = @"
SELECT [P].[id] AS [Id] FROM [Permissions] WHERE [P].[name] IN 
(VIEW_CASE, ASSIGN_LAWYER_CASE, ASSIGN_CUSTOMER_CASE, ASSIGN_CUSTOMER_CASE, GRANT_PERMISSIONS_CASE, REVOKE_PERMISSIONS_CASE)";

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

                    HasRevokePermissionOwnUserPermission                = permissionsResultSpecificUser.HasRevokePermissionsOwnUserPermission,
                    HasRevokePermissionOwnLawyerAccountUserPermission   = permissionsResultSpecificUser.HasRevokePermissionsOwnLawyerAccountUserPermission,
                    HasRevokePermissionOwnCustomerAccountUserPermission = permissionsResultSpecificUser.HasRevokePermissionsOwnCustomerAccountUserPermission,

                    HasRevokePermissionAnyUserPermission                = permissionsResultSpecificUser.HasRevokePermissionsAnyUserPermission,
                    HasRevokePermissionAnyLawyerAccountUserPermission   = permissionsResultSpecificUser.HasRevokePermissionsAnyLawyerAccountUserPermission,
                    HasRevokePermissionAnyCustomerAccountUserPermission = permissionsResultSpecificUser.HasRevokePermissionsAnyCustomerAccountUserPermission,

                    HasViewAnyUserPermission                = permissionsResultSpecificUser.HasViewAnyUserPermission,
                    HasViewAnyLawyerAccountUserPermission   = permissionsResultSpecificUser.HasViewAnyLawyerAccountUserPermission,
                    HasViewAnyCustomerAccountUserPermission = permissionsResultSpecificUser.HasViewAnyCustomerAccountUserPermission,

                    HasViewPublicUserPermission                = permissionsResultSpecificUser.HasViewPublicUserPermission,
                    HasViewPublicLawyerAccountUserPermission   = permissionsResultSpecificUser.HasViewPublicLawyerAccountUserPermission,
                    HasViewPublicCustomerAccountUserPermission = permissionsResultSpecificUser.HasViewPublicCustomerAccountUserPermission,

                    HasViewOwnUserPermission                = permissionsResultSpecificUser.HasViewOwnUserPermission,
                    HasViewOwnLawyerAccountUserPermission   = permissionsResultSpecificUser.HasViewOwnLawyerAccountUserPermission,
                    HasViewOwnCustomerAccountUserPermission = permissionsResultSpecificUser.HasViewOwnCustomerAccountUserPermission,

                    // [ACL]

                    ViewUserPermissionId                = permission.ViewUserPermissionId,
                    ViewLawyerAccountUserPermissionId   = permission.ViewUserPermissionId,
                    ViewCustomerAccountUserPermissionId = permission.ViewUserPermissionId,

                    RevokePermissionUserPermissionId                = permission.RevokePermissionsUserPermissionId,
                    RevokePermissionLawyerAccountUserPermissionId   = permission.RevokePermissionsLawyerAccountUserPermissionId,
                    RevokePermissionCustomerAccountUserPermissionId = permission.RevokePermissionsUserPermissionId,

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
                        @ViewUserPermissionId IS NOT NULL EXISTS (
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
                RelatedCaseId = parameters.CaseId,

                AttributeId  = x.AttributeId,
                PermissionId = x.PermissionId,
                UserId       = x.UserId,
                RoleId       = x.RoleId
            });

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"DELETE [permission_grants_case] WHERE [case_id] = @RelatedCaseId AND [user_id] = @UserId AND [permission_id] = @PermissionId AND [attribute_id] = @AttributeId;");

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

    //private async Task<bool> IsUserOwnerOfTheCaseAsync(
    //    int caseId,
    //    int userId,
    //    Contextualizer contextualizer)
    //{
    //    var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);
    //
    //    var queryParameters = new { CaseId = caseId, UserId = userId };
    //
    //    var queryText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [cases] C WHERE [C].[id] = @CaseId AND [C].[user_id] = @UserId) THEN 1 ELSE 0 END AS [Owner]";
    //    
    //    var result = await connection.Connection.QueryFirstAsync<bool>(
    //        new CommandDefinition(
    //                commandText:       queryText,
    //                parameters:        queryParameters,
    //                transaction:       connection.Transaction,
    //                cancellationToken: contextualizer.CancellationToken,
    //                commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
    //    
    //    return result;
    //}

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