using Dapper;
using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.Domain.Case.Interfaces.Services;
using LawyerCustomerApp.Domain.Case.Responses.Repositories.Error;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;
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
            resultConstructor.SetConstructor(
                new NotFoundDatabaseConnectionStringError()
                {
                    Status = 500
                });

            return resultConstructor.Build<SearchInformation>();
        }

        _databaseService.AppendConnectionStringWithIdentifier("local-sqlite", sqliteConnectionString, ProviderType.Sqlite);

        var connection = await _databaseService.GetConnection("local-sqlite", ProviderType.Sqlite);

        contextualizer.AssignContextualizedConnection(connection);

        var persona = parameters.GetPersonaIdentifier();
        var role    = "USER";

        int? roleId                     = await GetRoleIdAsync(role, contextualizer);
        int? viewAnyCasePermissionId    = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_CASE, contextualizer);
        int? viewPublicCasePermissionId = await GetPermissionIdAsync(PermissionSymbols.VIEW_ANY_PUBLIC_CASE, contextualizer);

        var isCapable = await CheckPersonaCapabilityAsync(parameters.UserId, persona, contextualizer);

        var information = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new
            {
                UserId = parameters.UserId,
                RoleId = roleId,

                IsCapable = isCapable,

                PersonaName = persona,

                ViewAnyCasePermissionId    = viewAnyCasePermissionId,
                ViewPublicCasePermissionId = viewPublicCasePermissionId,

                TitleFilter = string.IsNullOrWhiteSpace(parameters.Query) ? null : $"%{parameters.Query}%",

                Limit  = parameters.Pagination.End - parameters.Pagination.Begin + 1,
                Offset = parameters.Pagination.Begin - 1
            };

            var information = new SearchInformation
            {
                Items = new List<SearchInformation.ItemProperties>(),
            };

            var queryText = $@"
            WITH [FilteredCases] AS (
                SELECT [C].[id], [C].[user_id], [C].[private]
                FROM [cases] [C]
                WHERE (@TitleFilter IS NULL OR [C].[title] LIKE @TitleFilter)
                LIMIT @Limit OFFSET @Offset
            ),
            CaseViewPermissions AS (
                SELECT
                    [FC].[id] AS [case_id],

                    -- Flag 1: Can user view via Specific or Global means? (Ownership, ACL, UserGrant, RoleGrant)

                    MAX(CASE WHEN (

                        -- Layer 1: permission_grants_case (ACL)

                        EXISTS (
                            SELECT 1 FROM [permission_grants_case] [PGC]
                            LEFT JOIN [attributes] [A_PGC] ON [PGC].[attribute_id] = [A_PGC].[id]
                            WHERE 
                               [PGC].[case_id] = [FC].[id] AND
                               [PGC].[user_id] = @UserId   AND
                               [PGC].[role_id] = @RoleId   AND
                               ([PGC].[attribute_id] IS NULL OR ([A_PGC].[name] = @PersonaName AND @IsCapable = 1))
                        ) OR

                        -- Layer 2: Ownership

                        [FC].[user_id] = @UserId OR

                        -- Layer 3: permission_grants_user (Override for VIEW_ANY_CASE)

                        (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
                            SELECT 1 FROM [permission_grants_user] [PGU]
                            LEFT JOIN [attributes] [A_PGU] ON [PGU].[attribute_id] = [A_PGU].[id]
                            WHERE 
                               [PGU].[user_id]       = @UserId                  AND
                               [PGU].[permission_id] = @ViewAnyCasePermissionId AND
                               [PGU].[role_id]       = @RoleId                  AND
                               ([PGU].[attribute_id] IS NULL OR ([A_PGU].[name] = @PersonaName AND @IsCapable = 1))
                        )) OR

                        -- Layer 4: permission_grants (Role Grant for VIEW_ANY_CASE)

                        (@ViewAnyCasePermissionId IS NOT NULL AND EXISTS (
                            SELECT 1 FROM [permission_grants] [PG]
                            LEFT JOIN [attributes] [A_PG] 
                               ON [PG].[attribute_id] = [A_PG].[id]
                            WHERE 
                               [PG].[permission_id] = @ViewAnyCasePermissionId AND
                               [PG].[role_id]       = @RoleId                  AND
                               ([PG].[attribute_id] IS NULL OR ([A_PG].[name] = @PersonaName AND @IsCapable = 1))
                        ))
                    ) THEN 1 ELSE 0 END) AS [has_specific_or_global_grant],

                    -- Flag 2: Does user have grant for VIEW_ANY_PUBLIC_CASE? (Remains the same)

                    MAX(CASE WHEN (
                        @ViewPublicCasePermissionId IS NOT NULL AND (
                            EXISTS (
                                SELECT 1 FROM [permission_grants_user] [PGU_PUB]
                                LEFT JOIN [attributes] [A_PGU_PUB] 
                                   ON [PGU_PUB].[attribute_id] = [A_PGU_PUB].[id]
                                WHERE 
                                   [PGU_PUB].[user_id]       = @UserId                     AND
                                   [PGU_PUB].[permission_id] = @ViewPublicCasePermissionId AND
                                   [PGU_PUB].[role_id]       = @RoleId                     AND
                                   ([PGU_PUB].[attribute_id] IS NULL OR ([A_PGU_PUB].[name] = @PersonaName AND @IsCapable = 1))
                            ) OR
                            EXISTS (
                                SELECT 1 FROM [permission_grants] [PG_PUB]
                                LEFT JOIN [attributes] [A_PG_PUB] 
                                   ON [PG_PUB].[attribute_id] = [A_PG_PUB].[id]
                                WHERE 
                                   [PG_PUB].[permission_id] = @ViewPublicCasePermissionId AND
                                   [PG_PUB].[role_id]       = @RoleId                     AND
                                   ([PG_PUB].[attribute_id] IS NULL OR ([A_PG_PUB].[name] = @PersonaName AND @IsCapable = 1))
                            )
                        )
                    ) THEN 1 ELSE 0 END) AS [has_public_view_grant]

                FROM [FilteredCases] [FC]
                GROUP BY [FC].[id]
            )

            SELECT
                [C].[id] AS [CaseId], [C].[title] AS [Title], [C].[description] AS [Description], [C].[customer_id] AS [CustomerId], [C].[lawyer_id] AS [LawyerId], [C].[user_id] AS [UserId]
            FROM [cases] [C]
            JOIN CaseViewPermissions [CVP] 
               ON [C].[id] = [CVP].[case_id]
            WHERE
                [CVP].[has_specific_or_global_grant] = 1 OR
                ([C].[private] = 0 AND [CVP].[has_public_view_grant] = 1);
            
            /*
            SELECT 
               COUNT([C].[id]) AS [Count]
            FROM [cases] [C]
            JOIN CaseViewPermissions [CVP] 
               ON [C].[id] = [CVP].[case_id]
            WHERE
                [CVP].[has_specific_or_global_grant] = 1 OR
                ([C].[private] = 0 AND [CVP].[has_public_view_grant] = 1);

            */";

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
        async Task<Result<bool>> CheckPermissionAsync(
            int userId,
            string roleName,
            string personaName,
            string permissionName,
            Contextualizer contextualizer)
        {
            var resultConstructor = new ResultConstructor();

            var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);
        
            if (userId <= 0 || string.IsNullOrWhiteSpace(personaName) || string.IsNullOrWhiteSpace(permissionName))
                return resultConstructor.Build<bool>(false);
        
            var isCapable = await CheckPersonaCapabilityAsync(userId, personaName, contextualizer);

            var roleId = await GetRoleIdAsync(roleName, contextualizer);
            if (roleId == 0)
                return resultConstructor.Build<bool>(false);

            var permissionId = await GetPermissionIdAsync(permissionName, contextualizer);
            if (permissionId == 0)
                return resultConstructor.Build<bool>(false);

            // [Permission (from permission_grants_user)]

            var permissionGrantsUser = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    UserId       = userId,
                    RoleId       = roleId,
                    PermissionId = permissionId,
                    PersonaName  = personaName,
                    IsCapable    = isCapable
                };
        
                var queryText = @$"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM [permission_grants_user] [PGU]
                        LEFT JOIN [attributes] [A] 
                            ON [PGU].[attribute_id] = [A].[id]
                        WHERE 
                            [PGU].[user_id]       = @UserId       AND
                            [PGU].[permission_id] = @PermissionId AND
                            [PGU].[role_id]       = @RoleId       AND
                            ([PGU].[attribute_id] IS NULL OR ([A].[name] = @PersonaName AND @IsCapable = 1))
                    ) THEN 1 ELSE 0 END";
        
                var result = await connection.Connection.QueryFirstOrDefaultAsync<int>(
                    new CommandDefinition(
                            commandText:       queryText,
                            parameters:        queryParameters,
                            transaction:       connection.Transaction,
                            cancellationToken: contextualizer.CancellationToken,
                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
        
                return result;
            });
        
            if (permissionGrantsUser == 1)
                return resultConstructor.Build<bool>(true);

            // [Permission (from permission_grants)]

            var permissionGrants = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    UserId       = userId,
                    RoleId       = roleId,
                    PermissionId = permissionId,
                    PersonaName  = personaName,
                    IsCapable    = isCapable
        
                };
        
                var queryText = @$"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM [permission_grants] [PG]
                        LEFT JOIN [attributes] [A] 
                            ON [PG].[attribute_id] = [A].[id]
                        WHERE 
                            [PG].[permission_id] = @PermissionId AND
                            [PG].[role_id]       = @RoleId       AND
                            ([PG].[attribute_id] IS NULL OR ([A].[name] = @PersonaName AND @IsCapable = 1))
                    ) THEN 1 ELSE 0 END";
        
                var result = await connection.Connection.QueryFirstOrDefaultAsync<int>(
                    new CommandDefinition(
                            commandText:       queryText,
                            parameters:        queryParameters,
                            transaction:       connection.Transaction,
                            cancellationToken: contextualizer.CancellationToken,
                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));
        
                return result;
            });
        
            if (permissionGrants == 1)
                return resultConstructor.Build<bool>(true);

            return resultConstructor.Build<bool>(false);
        }

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

        var persona = parameters.GetPersonaIdentifier();
        var role    = "USER";

        var resultPermission = await CheckPermissionAsync(
                parameters.UserId,
                role,
                persona,
                PermissionSymbols.REGISTER_CASE,
                contextualizer);

        if (resultPermission.IsFinished)
            return resultConstructor.Build().Incorporate(resultPermission);

        if (!resultPermission.Value)
        {
            resultConstructor.SetConstructor(
                new RegisterCaseDeniedError()
                {
                    Status = 400
                });

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
            resultConstructor.SetConstructor(
                new RegisterCaseInsertionError()
                {
                    Status = 500
                });

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }

    public async Task<Result> AssignLawyerAsync(AssignLawyerParameters parameters, Contextualizer contextualizer)
    {
        async Task<Result<bool>> CheckPermissionAsync(
            int userId,
            int caseId,
            string roleName,
            string personaName,
            string permissionName,
            Contextualizer contextualizer)
        {
            var resultConstructor = new ResultConstructor();

            var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

            if (userId <= 0 || string.IsNullOrWhiteSpace(personaName) || string.IsNullOrWhiteSpace(permissionName))
                return resultConstructor.Build<bool>(false);

            var isCapable = await CheckPersonaCapabilityAsync(userId, personaName, contextualizer);

            var roleId = await GetRoleIdAsync(roleName, contextualizer);
            if (roleId == 0)
                return resultConstructor.Build<bool>(false);

            var permissionId = await GetPermissionIdAsync(permissionName, contextualizer);
            if (permissionId == 0)
                return resultConstructor.Build<bool>(false);

            // [Permission (from permission_grants_case) - (ACL)]  

            var permissionGrantsCase = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    UserId       = userId,
                    RoleId       = roleId,
                    CaseId       = caseId,
                    PermissionId = permissionId,
                    PersonaName  = personaName,
                    IsCapable    = isCapable
                };

                var queryText = @$"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM [permission_grants_case] [PGC]
                        LEFT JOIN [attributes] [A] 
                            ON [PGC].[attribute_id] = [A].[id]
                        WHERE 
                            [PGC].[case_id]       = @CaseId       AND
                            [PGC].[user_id]       = @UserId       AND
                            [PGC].[permission_id] = @PermissionId AND
                            [PGC].[role_id]       = @RoleId       AND
                            ([PGC].[attribute_id] IS NULL OR ([A].[name] = @PersonaName AND @IsCapable = 1))
                    ) THEN 1 ELSE 0 END";

                var result = await connection.Connection.QueryFirstOrDefaultAsync<int>(
                    new CommandDefinition(
                            commandText:       queryText,
                            parameters:        queryParameters,
                            transaction:       connection.Transaction,
                            cancellationToken: contextualizer.CancellationToken,
                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

                return result;
            });

            if (permissionGrantsCase == 1)
                return resultConstructor.Build<bool>(true);

            // [Permission (Ownership)]  

            var permissionOwnership = await ValuesExtensions.GetValue(async () =>
            {
                var queryParameters = new
                {
                    UserId = userId,
                    CaseId = caseId
                };

                var queryText = @$"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM [cases] [C]
                        WHERE [C].[id]      = @CaseId AND 
                              [C].[user_id] = @UserId
                    ) THEN 1 ELSE 0 END";

                var result = await connection.Connection.QueryFirstOrDefaultAsync<int>(
                    new CommandDefinition(
                            commandText:       queryText,
                            parameters:        queryParameters,
                            transaction:       connection.Transaction,
                            cancellationToken: contextualizer.CancellationToken,
                            commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

                return result;
            });

            if (permissionOwnership == 1)
                return resultConstructor.Build<bool>(true);

            return resultConstructor.Build<bool>(false);
        }

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

        var persona = parameters.GetPersonaIdentifier();
        var role    = "USER";

        var resultPermission = await CheckPermissionAsync(
            parameters.UserId,
            parameters.CaseId,
            role,
            persona,
            PermissionSymbols.ASSIGN_LAWYER_CASE,
            contextualizer);

        if (resultPermission.IsFinished)
            return resultConstructor.Build().Incorporate(resultPermission);

        if (!resultPermission.Value)
        {
            resultConstructor.SetConstructor(
                new AssignLawyerDeniedError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        var actualDate = DateTime.Now;

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
            resultConstructor.SetConstructor(
                new RegisterCaseInsertionError()
                {
                    Status = 500
                });

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
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

    private async Task<int> GetRoleIdAsync(
        string roleName,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        if (string.IsNullOrWhiteSpace(roleName)) return 0;

        var queryParameters = new { Name = roleName };

        var queryText = "SELECT [R].[id] FROM [roles] R WHERE [R].[name] = @Name LIMIT 1";

        var id = await connection.Connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                    commandText:       queryText,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        return id ?? 0;
    } 

    private async Task<bool> CheckPersonaCapabilityAsync(
        int userId,
        string personaName,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        if (string.IsNullOrWhiteSpace(personaName)) return true;

        string text;

        switch (personaName.ToUpperInvariant())
        {
            case "LAWYER":
                text = "SELECT 1 FROM [lawyers] WHERE [user_id] = @UserId LIMIT 1";
                break;
            case "CUSTOMER":
                text = "SELECT 1 FROM [customers] WHERE [user_id] = @UserId LIMIT 1";
                break;

            // << Add cases for other personas requiring checks >>
            // case "MONITOR":
            //     capabilityCheckSql = "SELECT 1 FROM [monitors] WHERE [user_id] = @UserId LIMIT 1";
            //     break;

            default:
                return false;
        }

        var parameters = new { UserId = userId };

        var result = await connection.Connection.QueryFirstOrDefaultAsync<int>(
                new CommandDefinition(
                        commandText:       text,
                        parameters:        parameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

        return result == 1;
    } 
}