using Dapper;
using LawyerCustomerApp.Domain.Common.Responses.Error;
using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.Domain.User.Interfaces.Services;
using LawyerCustomerApp.Domain.User.Responses.Repositories.Error;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System.Text;

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
            resultConstructor.SetConstructor(
                new EmailAlreadyInUseError()
                {
                    Status = 400
                });

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
            resultConstructor.SetConstructor(
                new UserInsertionError()
                {
                    Status = 500
                });

            return resultConstructor.Build();
        }
        return resultConstructor.Build();
    }
}