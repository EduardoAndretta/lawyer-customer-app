using LawyerCustomerApp.External.Database.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace LawyerCustomerApp.External.Interfaces;

public interface IDatabaseService
{
    void AppendConnectionStringWithIdentifier(
       string identifier,
       string connectionString,
       ProviderType providerType);

    Task<ConnectionWrapper> GetConnection(
        string connectionType,
        Guid sessionId,
        ProviderType providerType);
    Task<ConnectionWrapper> GetConnection(
        string connectionType,
        ProviderType providerType);

    Task<TReturn> Execute<TReturn>(
        ConnectionWrapper   connection,
        Func<Task<TReturn>> code,
        TransactionOptions? transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0);

    Task Execute(
        ConnectionWrapper   connection,
        Func<Task>          code,
        TransactionOptions? transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0);

    Task<ContextConnectionWrapper> GetContext<TContext>(
        string connectionType,
        Guid sessionId,
        ProviderType providerType) where TContext : DbContext;

    Task<ContextConnectionWrapper> GetContext<TContext>(
        string connectionType,
        ProviderType providerType) where TContext : DbContext;

    Task<TReturn> Execute<TReturn>(
        ContextConnectionWrapper connection,
        Func<Task<TReturn>>       code,
        TransactionOptions?       transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0);

    Task Execute(
        ContextConnectionWrapper connection,
        Func<Task>                code,
        TransactionOptions?       transactionOptions = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath]   string fileName   = "",
        [CallerLineNumber] int    lineNumber = 0);
}


