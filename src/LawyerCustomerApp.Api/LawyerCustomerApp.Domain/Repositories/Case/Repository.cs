using Dapper;
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

        var actualDate = DateTime.Now;

        var includedItems = await ValuesExtensions.GetValue(async () =>
        {
            var encrpytedTitle       = _hashService.Encrypt(parameters.Title);
            var encrpytedDescription = _hashService.Encrypt(parameters.Description);

            var queryParameters = new 
            {
                Title       = encrpytedTitle,
                Description = encrpytedDescription,
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

        var caseInformation = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new
            {
                CaseId = parameters.CaseId
            };

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(@"SELECT [T].[private] AS [Private] FROM [cases] C
                                   WHERE  [T].[id] = @CaseId");

            var caseInformation = await connection.Connection.QueryFirstOrDefaultAsync<AssignLawyerDatabaseInformation>(
                new CommandDefinition(
                        commandText:       stringBuilder.ToString(),
                        parameters:        queryParameters,
                        transaction:       connection.Transaction,
                        cancellationToken: contextualizer.CancellationToken,
                        commandTimeout:    TimeSpan.FromHours(1).Milliseconds));

            return caseInformation;
        });

        if (caseInformation == null)
        {
            resultConstructor.SetConstructor(
                new CaseNotFoundError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        var persona = parameters.GetPersonaIdentifier();

        var hasSpecificCasePermission = await CheckCaseSpecificPermissionAsync(
            parameters.UserId,
            parameters.CaseId,
            persona,
            PermissionSymbols.ASSIGN_LAWYER_CASE,
            PermissionSymbols.EDIT_ANY_CASE,
            contextualizer);

        if (caseInformation.Private && !hasSpecificCasePermission)
        {
            resultConstructor.SetConstructor(
                new AssignLawyerDeniedError()
                {
                    Status = 400
                });

            return resultConstructor.Build();
        }

        if (!caseInformation.Private && !hasSpecificCasePermission)
        {
            var hasOrdinaryPermission = await CheckOrdinaryPermissionAsync(
                parameters.UserId,
                persona,
                PermissionSymbols.VIEW_ANY_PUBLIC_CASE,
                PermissionSymbols.VIEW_ANY_CASE,
                contextualizer);

            if (!hasOrdinaryPermission)
            {
                resultConstructor.SetConstructor(
                    new AssignLawyerDeniedError()
                    {
                        Status = 400
                    });

                return resultConstructor.Build();
            }
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

    private async Task<bool> CheckCaseSpecificPermissionAsync(
        int userId,
        int caseId,
        string currentPersona,
        string requiredPermissionName,
        string globalOverridePermissionName,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var stringBuilder = new StringBuilder();
    
        if (string.IsNullOrWhiteSpace(currentPersona) || string.IsNullOrWhiteSpace(requiredPermissionName))
        {
            return false;
        }
    
        var requiredPermissionId = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new
            {
                RequiredPermissionName = requiredPermissionName
            };
    
            var query = "SELECT [P].[id] FROM [Permissions] P WHERE [P].[name] = @RequiredPermissionName LIMIT 1";
    
            return await connection.Connection.QueryFirstOrDefaultAsync<int>(
                new CommandDefinition(
                    commandText:       query,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromDays(1).Milliseconds
    
                ));
        });
    
        if (requiredPermissionId == 0)
            return false;
    
        stringBuilder.Append(@"
        SELECT EXISTS (
    
            -- 0. Check for Direct User Override Grant (considering ACTIVE persona)
    
            SELECT 1
            FROM [user_permission_overrides] [UPO]
            LEFT JOIN [user_attributes] [UA_OVERRIDE] 
                ON [UPO].[attribute_id] = [UA_OVERRIDE].[id]
            WHERE
                [UPO].[user_id]       = @UserId AND
                [UPO].[permission_id] = @RequiredPermissionId
    
                -- Check if override matches active persona OR is a generic override (attribute_id IS NULL)
    
                AND (
                    -- Generic override for the user
                    [UPO].[attribute_id] IS NULL OR 
    
                    -- Override specifically matches active persona
                    [UA_OVERRIDE].[name] = @CurrentPersonaName
                    )
    
                -- Optional Safety Check: Ensure user is capable of the persona if the override requires one
                AND (
                        -- No capability check needed for generic override
                        [UPO].[attribute_id] IS NULL OR
                        (@CurrentPersonaName = 'LAWYER'   AND EXISTS (SELECT 1 FROM [lawyers]   LAWY WHERE [LAWY].[user_id] = @UserId)) OR
                        (@CurrentPersonaName = 'CUSTOMER' AND EXISTS (SELECT 1 FROM [customers] CUST WHERE [CUST].[user_id] = @UserId))
                    )
    
            UNION ALL
    
            -- 1. Check for Global Override Permission (via Role/Attribute, considering ACTIVE persona)
    
            SELECT 1
            FROM [permission_grants] [PG]
            JOIN [user_roles] [UR] 
                ON [PG].[required_role_id] = [UR].[role_id] AND 
                   [UR].[user_id]          = @UserId
            JOIN [permissions] [P_GLOBAL] 
                ON [PG].[permission_id] = [P_GLOBAL].[id]
            LEFT JOIN [user_attributes] [UA]
                ON [PG].[required_attribute_id] = [UA].[id]
            WHERE
                [P_GLOBAL].[name] = @GlobalOverridePermissionName AND
                [UR].[user_id]    = @UserId AND
                (
                   [PG].[required_attribute_id] IS NULL OR
                   (
                       [PG].[required_attribute_id] IS NOT NULL AND [UA].[name] = @CurrentPersonaName AND
                       (
                           (@CurrentPersonaName = 'LAWYER'   AND EXISTS (SELECT 1 FROM [lawyers]   LAWY WHERE [LAWY].[user_id] = @UserId)) OR
                           (@CurrentPersonaName = 'CUSTOMER' AND EXISTS (SELECT 1 FROM [customers] CUST WHERE [CUST].[user_id] = @UserId))
                       )
                   )
                )
    
            UNION ALL
    
            -- 2. Check for Case Ownership
    
            SELECT 1
            FROM [cases] [C]
            WHERE 
                [C].[id]              = @CaseId AND 
                [C].[creator_user_id] = @UserId
    
            UNION ALL
    
            -- 3. Check for Specific Grant on this Case (ACL - Persona Dependent)
    
            SELECT 1
            FROM [case_user_permissions] [CUP]
            JOIN [permissions] [P_SPECIFIC] 
                ON [CUP].[permission_id] = [P_SPECIFIC].[id]
            LEFT JOIN [user_attributes] [UA_GRANT] 
                ON [CUP].[attribute_id] = [UA_GRANT].[id]
            WHERE
                [CUP].[case_id]   = @CaseId               AND
                [CUP].[user_id]   = @UserId               AND
                [P_SPECIFIC].[id] = @RequiredPermissionId AND
                ([CUP].[attribute_id] IS NULL OR [UA_GRANT].[name] = @CurrentPersonaName) AND
                (
                      [CUP].[attribute_id] IS NULL OR
                      (@CurrentPersonaName = 'LAWYER'   AND EXISTS (SELECT 1 FROM [lawyers]   LAWY WHERE [LAWY].[user_id] = @UserId)) OR
                      (@CurrentPersonaName = 'CUSTOMER' AND EXISTS (SELECT 1 FROM [customers] CUST WHERE [CUST].[user_id] = @UserId))
                )
        );");
    
        var parameters = new
        {
            UserId                       = userId,
            CaseId                       = caseId,
            RequiredPermissionId         = requiredPermissionId,
            CurrentPersonaName           = currentPersona,
            GlobalOverridePermissionName = globalOverridePermissionName
        };
    
        var result = await connection.Connection.QuerySingleOrDefaultAsync<int>(
             new CommandDefinition(
                 commandText:       stringBuilder.ToString(),
                 parameters:        parameters,
                 transaction:       connection.Transaction,
                 cancellationToken: contextualizer.CancellationToken,
                 commandTimeout:    TimeSpan.FromDays(1).Milliseconds
             ));
    
        return result > 0;
    }
    
    private async Task<bool> CheckOrdinaryPermissionAsync(
        int userId,
        string currentPersona,
        string requiredPermissionName,
        string globalOverridePermissionName,
        Contextualizer contextualizer)
    {
        var connection = await contextualizer.ConnectionContextualizer.GetConnection(_databaseService, ProviderType.Sqlite);

        var stringBuilder = new StringBuilder();
    
        if (string.IsNullOrWhiteSpace(currentPersona) || string.IsNullOrWhiteSpace(requiredPermissionName))
        {
            return false;
        }
    
        var requiredPermissionId = await ValuesExtensions.GetValue(async () =>
        {
            var queryParameters = new
            {
                RequiredPermissionName = requiredPermissionName
            };
    
            var query = "SELECT [P].[id] FROM [Permissions] P WHERE [P].[name] = @RequiredPermissionName LIMIT 1";
    
            return await connection.Connection.QueryFirstOrDefaultAsync<int>(
                new CommandDefinition(
                    commandText:       query,
                    parameters:        queryParameters,
                    transaction:       connection.Transaction,
                    cancellationToken: contextualizer.CancellationToken,
                    commandTimeout:    TimeSpan.FromDays(1).Milliseconds
    
                ));
        });
    
        if (requiredPermissionId == 0)
            return false;
    
        stringBuilder.Append(@"
        SELECT EXISTS (
            -- 0. Check for Direct User Override Grant (considering ACTIVE persona)
    
            SELECT 1
            FROM [user_permission_overrides] [UPO]
            LEFT JOIN [user_attributes] [UA_OVERRIDE] 
                ON [UPO].[attribute_id] = [UA_OVERRIDE].[id]
            WHERE
                [UPO].[user_id]       = @UserId AND
                [UPO].[permission_id] = @RequiredPermissionId
    
                -- Check if override matches active persona OR is a generic override (attribute_id IS NULL)
    
                AND (
                    -- Generic override for the user
                    [UPO].[attribute_id] IS NULL OR 
    
                    -- Override specifically matches active persona
                    [UA_OVERRIDE].[name] = @CurrentPersonaName
                    )
    
                -- Optional Safety Check: Ensure user is capable of the persona if the override requires one
                AND (
                        -- No capability check needed for generic override
                        [UPO].[attribute_id] IS NULL OR
                        (@CurrentPersonaName = 'LAWYER'   AND EXISTS (SELECT 1 FROM [lawyers]   LAWY WHERE [LAWY].[user_id] = @UserId)) OR
                        (@CurrentPersonaName = 'CUSTOMER' AND EXISTS (SELECT 1 FROM [customers] CUST WHERE [CUST].[user_id] = @UserId))
                    )
    
            UNION ALL
    
            -- 1. Check for Global Override Permission (via Role/Attribute, considering ACTIVE persona)
    
            SELECT 1
            FROM [permission_grants] [PG]
            JOIN [user_roles] [UR] 
                ON [PG].[required_role_id] = [UR].[role_id] AND 
                   [UR].[user_id]          = @UserId
            JOIN [permissions] [P_GLOBAL] 
                ON [PG].[permission_id] = [P_GLOBAL].[id]
            LEFT JOIN [user_attributes] [UA]
                ON [PG].[required_attribute_id] = [UA].[id]
            WHERE
                [P_GLOBAL].[name] = @GlobalOverridePermissionName AND
                [UR].[user_id]    = @UserId AND
                (
                   [PG].[required_attribute_id] IS NULL OR
                   (
                       [PG].[required_attribute_id] IS NOT NULL AND [UA].[name] = @CurrentPersonaName AND
                       (
                           (@CurrentPersonaName = 'LAWYER'   AND EXISTS (SELECT 1 FROM [lawyers]   LAWY WHERE [LAWY].[user_id] = @UserId)) OR
                           (@CurrentPersonaName = 'CUSTOMER' AND EXISTS (SELECT 1 FROM [customers] CUST WHERE [CUST].[user_id] = @UserId))
                       )
                   )
                )
        );");
    
        var parameters = new
        {
            UserId                       = userId,
            RequiredPermissionId         = requiredPermissionId,
            CurrentPersonaName           = currentPersona,
            GlobalOverridePermissionName = globalOverridePermissionName
        };
    
        var result = await connection.Connection.QuerySingleOrDefaultAsync<int>(
             new CommandDefinition(
                 commandText:       stringBuilder.ToString(),
                 parameters:        parameters,
                 transaction:       connection.Transaction,
                 cancellationToken: contextualizer.CancellationToken,
                 commandTimeout:    TimeSpan.FromDays(1).Milliseconds
             ));
    
        return result > 0;
    }
}