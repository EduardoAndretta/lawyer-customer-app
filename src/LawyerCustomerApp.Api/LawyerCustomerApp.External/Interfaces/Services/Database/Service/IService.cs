using LawyerCustomerApp.External.Database.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LawyerCustomerApp.External.Database.Interfaces;

internal interface IService
{
    void AppendConnectionStringWithIdentifier(string identifier, string connectionString);

    Task<ConnectionWrapper> GetConnection(string connectionType, Guid sessionId = default);

    Task<TReturn> Execute<TReturn>(
        ConnectionWrapper   connection,
        Func<Task<TReturn>> code,
        TransactionOptions? transactionOptions = null,  
        string memberName   = "",
        string fileName     = "",
        int    lineNumber   = 0);

    Task Execute(
        ConnectionWrapper   connection,
        Func<Task>          code,
        TransactionOptions? transactionOptions = null,  
        string memberName   = "",
        string fileName     = "",
        int    lineNumber   = 0);

    #region Context Management

    Task<ContextConnectionWrapper> GetContext<TContext>(
        string connectionType,
        Guid sessionId = default) where TContext : DbContext;

    Task<TReturn> Execute<TReturn>(
        ContextConnectionWrapper  contextConnection,
        Func<Task<TReturn>>       code,
        TransactionOptions?       transactionOptions = null,  
        string memberName   = "",
        string fileName     = "",
        int    lineNumber   = 0);

    Task Execute(
        ContextConnectionWrapper  contextConnection,
        Func<Task>                code,
        TransactionOptions?       transactionOptions = null,  
        string memberName   = "",
        string fileName     = "",
        int    lineNumber   = 0);

    #endregion
}


