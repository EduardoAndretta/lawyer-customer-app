using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

using Contracts = LawyerCustomerApp.External.Database.Interfaces;

namespace LawyerCustomerApp.External.Database.Services;

public class Service : IDatabaseService
{
    private readonly IServiceProvider _serviceProvider;
    public Service(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void AppendConnectionStringWithIdentifier(
        string identifier, 
        string connectionString, 
        ProviderType providerType)
    {
        var key = providerType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        variation.AppendConnectionStringWithIdentifier(identifier, connectionString);
    }

    public async Task<ConnectionWrapper> GetConnection(
        string         connectionType, 
        Guid           sessionId,
        ProviderType   providerType)
    {

        var key = providerType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        return await variation.GetConnection(connectionType, sessionId);
    }

    public async Task<ConnectionWrapper> GetConnection(
        string       connectionType, 
        ProviderType providerType)
    {
        var key = providerType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        return await variation.GetConnection(connectionType);
    }

    public async Task<TReturn> Execute<TReturn>(
        ConnectionWrapper   connection,
        Func<Task<TReturn>> code,
        TransactionOptions? transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0)
    {
        var key = connection.ProviderType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        return await variation.Execute(connection, code, transactionOptions, memberName, fileName, lineNumber);
    }

    public async Task Execute(
        ConnectionWrapper   connection,
        Func<Task>          code,
        TransactionOptions? transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0)
    {
        var key = connection.ProviderType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        await variation.Execute(connection, code, transactionOptions, memberName, fileName, lineNumber);
    }

    public async Task<ContextConnectionWrapper> GetContext<TContext>(
        string       connectionType,
        Guid         sessionId,
        ProviderType providerType) where TContext : DbContext
    {
        var key = providerType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        return await variation.GetContext<TContext>(connectionType, sessionId);
    }

    public async Task<ContextConnectionWrapper> GetContext<TContext>(
        string       connectionType,
        ProviderType providerType) where TContext : DbContext
    {
        var key = providerType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        return await variation.GetContext<TContext>(connectionType);
    }


    public async Task<TReturn> Execute<TReturn>(
        ContextConnectionWrapper  contextConnection,
        Func<Task<TReturn>>       code,
        TransactionOptions?       transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0)
    {
        var key = contextConnection.Connection.ProviderType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        return await variation.Execute(contextConnection, code, transactionOptions, memberName, fileName, lineNumber);
    }

    public async Task Execute(
        ContextConnectionWrapper  contextConnection,
        Func<Task>                code,
        TransactionOptions?       transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0)
    {
        var key = contextConnection.Connection.ProviderType switch
        {
            ProviderType.Sqlite => Keys.Key.CreateKey(Keys.Key.ProviderType.Sqlite),
            _ => throw new NotImplementedException()
        };

        var variation = _serviceProvider.GetRequiredKeyedService<Contracts.IService>(key.GetIdentifier());

        await variation.Execute(contextConnection, code, transactionOptions, memberName, fileName, lineNumber);
    }
}
