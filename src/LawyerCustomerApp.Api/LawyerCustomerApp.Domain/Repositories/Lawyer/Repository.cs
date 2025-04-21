using Dapper;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.Lawyer.Common.Models;
using LawyerCustomerApp.Domain.Lawyer.Interfaces.Services;
using LawyerCustomerApp.Domain.Lawyer.Responses.Repositories.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System.Text;

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

            stringBuilder.Append(@"SELECT CASE WHEN EXISTS (SELECT 1 FROM [lawyers] C WHERE [C].[user_id] = @UserId) 
                                        THEN 1 ELSE 0 
                                   END AS [user_already_have_lawyer_account]");

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
            resultConstructor.SetConstructor(
                new RegisterLawyerInsertionError()
                {
                    Status = 500
                });

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }
}