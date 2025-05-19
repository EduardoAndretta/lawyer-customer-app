using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Database.Interfaces;
using LawyerCustomerApp.External.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using SQLitePCL;
using System.Collections.Concurrent;
using System.Data;

using Variation = LawyerCustomerApp.External.Database.Sqlite.Models;

namespace LawyerCustomerApp.External.Database.Services.Sqlite;

internal class Service : IService
{
    private record SessionSpecification
    {
        public IEnumerable<Variation.ConnectionWrapper> Connections { get; init; } = new List<Variation.ConnectionWrapper>();
        public IEnumerable<Variation.ContextConnectionWrapper> ContextConnections { get; init; } = new List<Variation.ContextConnectionWrapper>();
    }

    private readonly Guid         _default      = Guid.NewGuid();
    private readonly ProviderType _providerType = ProviderType.Sqlite;

    private readonly object _lockObject = new();

    private readonly ConcurrentDictionary<string, string> _runtimeConnectionsKey = new ConcurrentDictionary<string, string>();

    private readonly ConcurrentDictionary<Guid, object> _connectionLocks     = new ConcurrentDictionary<Guid, object>();
    private readonly AsyncLocal<Guid?>                  _currentConnectionId = new AsyncLocal<Guid?>();

    private object GetConnectionLock(Variation.ConnectionWrapper conn) =>
        _connectionLocks.GetOrAdd(conn.ConnectionIdentifier, _ => new object());

    private readonly ConcurrentDictionary<Guid, SessionSpecification> _sessions = new();

    private readonly IConfiguration   _configuration;
    private readonly IServiceProvider _serviceProvider;

    private readonly IHashService _hashService;

    public Service(IConfiguration configuration, IServiceProvider serviceProvider, IHashService hashService)
    {
        _configuration   = configuration;
        _serviceProvider = serviceProvider;

        _hashService = hashService;

        Batteries.Init();
    }

    public void AppendConnectionStringWithIdentifier(string identifier, string connectionString)
    {
        var encryptedConnectionString = _hashService.Encrypt(connectionString);

        _runtimeConnectionsKey.TryAdd(identifier, encryptedConnectionString);
    }

    public async Task<ConnectionWrapper> GetConnection(string connectionType, Guid sessionId = default)
    {
        if (sessionId == default)
            sessionId = _default;

        if (!_sessions.TryGetValue(sessionId, out _))
            _sessions.TryAdd(sessionId, new());

        // [Look for an existing Connection of the given type]
        var connection = _sessions[sessionId].Connections.FirstOrDefault(x => x is Variation.CustomConnection comparerVariation && comparerVariation.ConnectionType == connectionType);
        if (connection != null)
            return connection;

        if (!_runtimeConnectionsKey.TryGetValue(connectionType, out var connectionString))
            throw new Exception($"Key of Connection Type not supported. {connectionType}.");

        lock (_lockObject)
        {
            var decryptedConnectionString = _hashService.Decrypt(connectionString);

            var sqlConnection = new SqliteConnection(decryptedConnectionString);

            connection = new Variation.CustomConnection(sqlConnection, sessionId, connectionType);
        }

        var clientConnectionId = await connection.GetConnectionId();

        _sessions.AddOrUpdate(
            sessionId,
            key => new SessionSpecification
            {
                Connections        = new List<Variation.ConnectionWrapper> { connection },
                ContextConnections = new List<Variation.ContextConnectionWrapper>()
            },

            (key, currentSession) =>
            {
                var updatedConnections = currentSession.Connections
                    .Append(connection)
                    .ToList();

                return currentSession with { Connections = updatedConnections };
            });

        return connection;
    }

    private Variation.ConnectionWrapper GetSessionConnection(Guid sessionId, ConnectionWrapper connection)
    {
        var sessionConnection = connection switch
        {
            Variation.CustomConnection variation => _sessions[sessionId].Connections.FirstOrDefault(x => x is Variation.CustomConnection variationComparer && variationComparer.ConnectionType == variation.ConnectionType)
                    ?? throw new Exception("The provided non typed connection was not mapped."),

            _ => throw new NotImplementedException("Connection Type not implemented.")
        };

        return sessionConnection;
    }

    private void EnforceCurrent(Guid sessionId, Variation.ConnectionWrapper connection)
    {
        lock (GetConnectionLock(connection))
        {
            connection.Current = true;

            _sessions.AddOrUpdate(
                sessionId,

                key => new SessionSpecification
                {
                    Connections        = new List<Variation.ConnectionWrapper> { connection },
                    ContextConnections = new List<Variation.ContextConnectionWrapper>()
                },

                (key, currentSession) =>
                {
                    var updatedConnections = currentSession.Connections
                        .Where(c => c.ConnectionIdentifier != connection.ConnectionIdentifier)
                        .Append(connection)
                        .ToList();

                    return currentSession with { Connections = updatedConnections };
                });

            UpdateContexts(connection, Command.SubscribeConnection, sessionId);

            _currentConnectionId.Value = connection.ConnectionIdentifier;
        }
    }

    private void EnforceNonCurrent(Guid sessionId, Variation.ConnectionWrapper connection)
    {
        lock (GetConnectionLock(connection))
        {
            connection.Current = false;

            _sessions.AddOrUpdate(
                sessionId,

                key => new SessionSpecification
                {
                    Connections        = new List<Variation.ConnectionWrapper> { connection },
                    ContextConnections = new List<Variation.ContextConnectionWrapper>()
                },

                (key, currentSession) =>
                {
                    var updatedConnections = currentSession.Connections
                        .Where(c => c.ConnectionIdentifier != connection.ConnectionIdentifier)
                        .Append(connection)
                        .ToList();

                    return currentSession with { Connections = updatedConnections };
                });

            UpdateContexts(connection, Command.SubscribeConnection, sessionId);

            _currentConnectionId.Value = null;
        }
    }


    private void EnforceTransactionAddition(Guid sessionId, Variation.ConnectionWrapper connection, Variation.TransactionWrapper transactionWrapper)
    {
        connection.SetTransaction(transactionWrapper);

        _sessions.AddOrUpdate(
            sessionId,

            key => new SessionSpecification
            {
                Connections        = new List<Variation.ConnectionWrapper> { connection },
                ContextConnections = new List<Variation.ContextConnectionWrapper>()
            },

            (key, currentSession) =>
            {
                var updatedConnections = currentSession.Connections
                    .Where(c => c.ConnectionIdentifier != connection.ConnectionIdentifier)
                    .Append(connection)
                    .ToList();

                return currentSession with { Connections = updatedConnections };
            });

        UpdateContexts(connection, Command.AddTransaction, sessionId);
    }

    private void EnforceTransactionRemoval(Guid sessionId, Variation.ConnectionWrapper connection, Variation.TransactionWrapper transactionWrapper)
    {
        transactionWrapper.Dispose();

        connection.RemoveTransaction();

        _sessions.AddOrUpdate(
            sessionId,

            key => new SessionSpecification
            {
                Connections        = new List<Variation.ConnectionWrapper> { connection },
                ContextConnections = new List<Variation.ContextConnectionWrapper>()
            },

            (key, currentSession) =>
            {
                var updatedConnections = currentSession.Connections
                    .Where(c => c.ConnectionIdentifier != connection.ConnectionIdentifier)
                    .Append(connection)
                    .ToList();

                return currentSession with { Connections = updatedConnections };
            });

        UpdateContexts(connection, Command.RemoveTransaction, sessionId);
    }

    public async Task<TReturn> Execute<TReturn>(
        ConnectionWrapper   connection,
        Func<Task<TReturn>> code,
        TransactionOptions? transactionOptions = null,
        string memberName = "",
        string fileName   = "",
        int    lineNumber = 0)
    {
        var sessionId = connection.SessionId;
        if (!_sessions.TryGetValue(sessionId, out _))
            throw new Exception($"Session not found for Id {sessionId}.");

        // [Determine if this call is already executing on the same connection]
        bool isNested = _currentConnectionId.Value == connection.ConnectionIdentifier;

        // [Retrieve the session connection based on the type]
        var sessionConnection = GetSessionConnection(sessionId, connection);

        // [Only the outer (non-nested) call should incorporate the connection]
        if (!isNested)
        {
            if (sessionConnection.Current)
                throw new Exception("This connection already in use. Create another session to realize parallel operations.");

            EnforceCurrent(sessionId, sessionConnection);
        }

        if (sessionConnection.Connection.State == ConnectionState.Broken)
            throw new Exception("The provided connection is broken.");

        var clientConnectionId = await sessionConnection.GetConnectionId();
       
        if (sessionConnection.TransactionWrapper != null && !sessionConnection.TransactionWrapper.IsTransactionDead)
        {
            if (sessionConnection.Connection.State == ConnectionState.Closed)
                throw new Exception("The provided connection is dirty. [Closed]");

            TReturn result = await code();

            return result;
        }

        if (transactionOptions != null)
        {
            if (sessionConnection.Connection.State != ConnectionState.Open)
                await sessionConnection.Connection.OpenAsync();

            using var sqlTransaction = await sessionConnection.Connection.BeginTransactionAsync(transactionOptions.IsolationLevel);
            
            var parsedSqlTransaction = sqlTransaction as SqliteTransaction
                ?? throw new NotSupportedException($"Context Transaction type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");
            
            var transactionWrapper = new Variation.TransactionWrapper(parsedSqlTransaction);

            EnforceTransactionAddition(sessionId, sessionConnection, transactionWrapper);

            try
            {
                TReturn result = await code();

                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.CommitAsync();
                }

                return result;
            }
            catch
            {
                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                EnforceTransactionRemoval(sessionId, sessionConnection, transactionWrapper);

                if (!isNested)
                    EnforceNonCurrent(sessionId, sessionConnection);
            }
        }
        else
        {
            TReturn result = await code();

            if (!isNested)
                EnforceNonCurrent(sessionId, sessionConnection);

            return result;
        }
    }

    public async Task Execute(
        ConnectionWrapper   connection,
        Func<Task>          code,
        TransactionOptions? transactionOptions = null,
        string memberName = "",
        string fileName   = "",
        int    lineNumber = 0)
    {
        var sessionId = connection.SessionId;
        if (!_sessions.TryGetValue(sessionId, out _))
            throw new Exception($"Session not found for Id {sessionId}.");

        bool isNested = _currentConnectionId.Value == connection.ConnectionIdentifier;

        var sessionConnection = GetSessionConnection(sessionId, connection);

        if (!isNested)
        {
            if (sessionConnection.Current)
                throw new Exception("This connection already in use. Create another session to realize parallel operations.");

            EnforceCurrent(sessionId, sessionConnection);
        }

        if (sessionConnection.Connection.State == ConnectionState.Broken)
            throw new Exception("The provided connection is broken.");

        var clientConnectionId = await sessionConnection.GetConnectionId();
        
        if (sessionConnection.TransactionWrapper != null && !sessionConnection.TransactionWrapper.IsTransactionDead)
        {
            if (sessionConnection.Connection.State == ConnectionState.Closed)
                throw new Exception("The provided connection is dirty. [Closed]");

            await code();

            return;
        }

        if (transactionOptions != null)
        {  
            if (sessionConnection.Connection.State != ConnectionState.Open)
                await sessionConnection.Connection.OpenAsync();

            using var sqlTransaction = await sessionConnection.Connection.BeginTransactionAsync(transactionOptions.IsolationLevel);
            
            var parsedSqlTransaction = sqlTransaction as SqliteTransaction
                ?? throw new NotSupportedException($"Context Transaction type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");
            
            var transactionWrapper = new Variation.TransactionWrapper(parsedSqlTransaction);

            EnforceTransactionAddition(sessionId, sessionConnection, transactionWrapper);

            try
            {
                await code();

                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.CommitAsync();
                }

                return;
            }
            catch
            {
                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                EnforceTransactionRemoval(sessionId, sessionConnection, transactionWrapper);

                if (!isNested)
                    EnforceNonCurrent(sessionId, sessionConnection);
            }
        }
        else
        {
            await code();

            if (!isNested)
                EnforceNonCurrent(sessionId, sessionConnection);

            return;
        }
    }

    #region Context Management

    public async Task<ContextConnectionWrapper> GetContext<TContext>(
        string connectionType,
        Guid sessionId = default) where TContext : DbContext
    {
        if (sessionId == default)
            sessionId = _default;

        if (!_sessions.TryGetValue(sessionId, out _))
            _sessions.TryAdd(sessionId, new());

        var connection = await GetConnection(connectionType, sessionId);

        var sessionConnection = GetSessionConnection(sessionId, connection);

        var existingContextSpec = _sessions[sessionId].ContextConnections.FirstOrDefault(c => c.Connection.ConnectionIdentifier == sessionConnection.ConnectionIdentifier && c.ContextType == typeof(TContext));
        if (existingContextSpec != null)
            return existingContextSpec;

        var sqlConnection = sessionConnection.Connection as SqliteConnection
             ?? throw new NotSupportedException($"Context Connection type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder
            .UseSqlite(sqlConnection);

        var context = (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;

        if (sessionConnection.TransactionWrapper != null)
            await context.Database.UseTransactionAsync(sessionConnection.TransactionWrapper.Transaction);

        var contextConnection = new Variation.ContextConnectionWrapper(context, sessionConnection, typeof(TContext), sessionId);

        _sessions.AddOrUpdate(
            sessionId,
            key => new SessionSpecification
            {
                Connections        = new List<Variation.ConnectionWrapper> { sessionConnection },
                ContextConnections = new List<Variation.ContextConnectionWrapper> { contextConnection }
            },

            (key, currentSession) =>
            {
                var updatedContextsConnections = currentSession.ContextConnections
                    .Append(contextConnection)
                    .ToList();

                return currentSession with { ContextConnections = updatedContextsConnections };
            });

        return contextConnection;
    }

    public async Task<TReturn> Execute<TReturn>(
        ContextConnectionWrapper contextConnection,
        Func<Task<TReturn>>       code,
        TransactionOptions?       transactionOptions = null,
        string memberName   = "",
        string fileName     = "",
        int lineNumber      = 0)
    {
        var sessionId = contextConnection.SessionId;
        if (!_sessions.TryGetValue(sessionId, out _))
            throw new Exception($"Session not found for Id {sessionId}.");

        bool isNested = _currentConnectionId.Value == contextConnection.Connection.ConnectionIdentifier;

        var sessionConnection = GetSessionConnection(sessionId, contextConnection.Connection);

        if (!isNested)
        {
            if (sessionConnection.Current)
                throw new Exception("This connection already in use. Create another session to realize parallel operations.");

            EnforceCurrent(sessionId, sessionConnection);
        }

        if (sessionConnection.Connection.State == ConnectionState.Broken)
            throw new Exception("The provided connection is broken.");

        var relatedDbConnection = contextConnection.Context.Database.GetDbConnection();
        if (relatedDbConnection != null)
        {
            var relatedConnection = relatedDbConnection as SqliteConnection
               ?? throw new NotSupportedException($"Context Connection type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

            if (relatedConnection != sessionConnection.Connection)
                throw new InvalidOperationException("The provided context connection are dirty. (Changes was made outside of service [Related Connection])");
        }
        else
        {
            throw new InvalidOperationException("The provided context does not have a connection. (Changes was made outside of service [Related Connection])");
        }

        var relatedDbTransaction = contextConnection.Context.Database.CurrentTransaction?.GetDbTransaction();
        if (relatedDbTransaction != null)
        {
            if (sessionConnection.Connection.State == ConnectionState.Closed)
                throw new Exception("The provided connection are dirty. [CLosed]");

            var relatedTransaction = relatedDbTransaction as SqliteTransaction
                ?? throw new NotSupportedException($"Context Transaction type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

            if ((relatedTransaction == null && sessionConnection.TransactionWrapper != null) ||
                (relatedTransaction != null && sessionConnection.TransactionWrapper == null))
                throw new InvalidOperationException("The provided context transaction are dirty. (Changes was made outside of service [State])");

            var transactionRelatedConnection = relatedTransaction?.Connection;
            if (sessionConnection.TransactionWrapper != null && transactionRelatedConnection != sessionConnection.Connection)
                throw new InvalidOperationException("The provided context transaction are dirty. (Changes was made outside of service [Related Transaction Connection])");
        }
        else
        {
            if ((relatedDbTransaction == null && sessionConnection.TransactionWrapper != null) ||
                (relatedDbTransaction != null && sessionConnection.TransactionWrapper == null))
                throw new InvalidOperationException("The provided context transaction are dirty. (Changes was made outside of service [State])");
        }

        var clientConnectionId = await sessionConnection.GetConnectionId();

        if (sessionConnection.TransactionWrapper != null && !sessionConnection.TransactionWrapper.IsTransactionDead)
        {
            if (sessionConnection.Connection.State == ConnectionState.Closed)
                throw new Exception("The provided connection is dirty. [CLosed]");

            var result = await code();

            return result;
        }

        if (transactionOptions != null)
        {
            if (sessionConnection.Connection.State != ConnectionState.Open)
                await sessionConnection.Connection.OpenAsync();

            using var sqlTransaction = await sessionConnection.Connection.BeginTransactionAsync(transactionOptions.IsolationLevel);

            var parsedSqlTransaction = sqlTransaction as SqliteTransaction
               ?? throw new NotSupportedException($"Context Transaction type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

            var transactionWrapper = new Variation.TransactionWrapper(parsedSqlTransaction);

            EnforceTransactionAddition(sessionId, sessionConnection, transactionWrapper);

            try
            {
                var result = await code();

                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.CommitAsync();
                }

                return result;
            }
            catch
            {
                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                EnforceTransactionRemoval(sessionId, sessionConnection, transactionWrapper);

                if (!isNested)
                    EnforceNonCurrent(sessionId, sessionConnection);
            }
        }
        else
        {
            var result = await code();

            if (!isNested)
                EnforceNonCurrent(sessionId, sessionConnection);

            return result;
        }
    }

    public async Task Execute(
        ContextConnectionWrapper contextConnection,
        Func<Task>                code,
        TransactionOptions?       transactionOptions = null,
        string memberName   = "",
        string fileName     = "",
        int lineNumber      = 0)
    {
        var sessionId = contextConnection.SessionId;
        if (!_sessions.TryGetValue(sessionId, out _))
            throw new Exception($"Session not found for Id {sessionId}.");

        bool isNested = _currentConnectionId.Value == contextConnection.Connection.ConnectionIdentifier;

        var sessionConnection = GetSessionConnection(sessionId, contextConnection.Connection);

        if (!isNested)
        {
            if (sessionConnection.Current)
                throw new Exception("This connection already in use. Create another session to realize parallel operations.");

            EnforceCurrent(sessionId, sessionConnection);
        }

        if (sessionConnection.Connection.State == ConnectionState.Broken)
            throw new Exception("The provided connection is broken.");

        var relatedDbConnection = contextConnection.Context.Database.GetDbConnection();
        if (relatedDbConnection != null)
        {
            var relatedConnection = relatedDbConnection as SqliteConnection
               ?? throw new NotSupportedException($"Context Connection type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

            if (relatedConnection != sessionConnection.Connection)
                throw new InvalidOperationException("The provided context connection are dirty. (Changes was made outside of service [Related Connection])");
        }
        else
        {
            throw new InvalidOperationException("The provided context does not have a connection. (Changes was made outside of service [Related Connection])");
        }

        var relatedDbTransaction = contextConnection.Context.Database.CurrentTransaction?.GetDbTransaction();
        if (relatedDbTransaction != null)
        {
            if (sessionConnection.Connection.State == ConnectionState.Closed)
                throw new Exception("The provided connection are dirty. [CLosed]");

            var relatedTransaction = relatedDbTransaction as SqliteTransaction
                ?? throw new NotSupportedException($"Context Transaction type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

            if ((relatedTransaction == null && sessionConnection.TransactionWrapper != null) ||
                (relatedTransaction != null && sessionConnection.TransactionWrapper == null))
                throw new InvalidOperationException("The provided context transaction are dirty. (Changes was made outside of service [State])");

            var transactionRelatedConnection = relatedTransaction?.Connection;
            if (sessionConnection.TransactionWrapper != null && transactionRelatedConnection != contextConnection.Connection.Connection)
                throw new InvalidOperationException("The provided context transaction are dirty. (Changes was made outside of service [Related Transaction Connection])");
        }
        else
        {
            if ((relatedDbTransaction == null && sessionConnection.TransactionWrapper != null) ||
                (relatedDbTransaction != null && sessionConnection.TransactionWrapper == null))
                throw new InvalidOperationException("The provided context transaction are dirty. (Changes was made outside of service [State])");
        }

        var clientConnectionId = await contextConnection.Connection.GetConnectionId();

        if (sessionConnection.TransactionWrapper != null && !sessionConnection.TransactionWrapper.IsTransactionDead)
        {
            if (sessionConnection.Connection.State == ConnectionState.Closed)
                throw new Exception("The provided connection is dirty. [CLosed]");

            await code();

            return;
        }

        if (transactionOptions != null)
        {
            if (sessionConnection.Connection.State != ConnectionState.Open)
                await sessionConnection.Connection.OpenAsync();

            using var sqlTransaction = await sessionConnection.Connection.BeginTransactionAsync(transactionOptions.IsolationLevel);

            var parsedSqlTransaction = sqlTransaction as SqliteTransaction
               ?? throw new NotSupportedException($"Context Transaction type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

            var transactionWrapper = new Variation.TransactionWrapper(parsedSqlTransaction);

            EnforceTransactionAddition(sessionId, sessionConnection, transactionWrapper);

            try
            {
                await code();

                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.CommitAsync();
                }

                return;
            }
            catch
            {
                if (transactionOptions.ExecuteRollbackAndCommit)
                {
                    UpdateContexts(sessionConnection, Command.SaveChangesAsync, sessionId);
                    await parsedSqlTransaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                EnforceTransactionRemoval(sessionId, sessionConnection, transactionWrapper);

                if (!isNested)
                    EnforceNonCurrent(sessionId, sessionConnection);
            }
        }
        else
        {
            await code();

            if (!isNested)
                EnforceNonCurrent(sessionId, sessionConnection);

            return;
        }
    }

    private enum Command
    {
        AddTransaction,
        RemoveTransaction,
        SubscribeConnection,
        SaveChangesAsync
    }

    private void UpdateContexts(Variation.ConnectionWrapper connection, Command command, Guid sessionId)
    {
        var contextConnections = _sessions[sessionId].ContextConnections
        .Where(x => x.Connection.ConnectionIdentifier == connection.ConnectionIdentifier)
        .ToList();

        var nonChangedContextConnections = _sessions[sessionId].ContextConnections
        .Where(x => x.Connection.ConnectionIdentifier != connection.ConnectionIdentifier)
        .ToList();

        if (!contextConnections.Any())
            return;

        List<Variation.ContextConnectionWrapper> changedContextConnections = new();

        foreach (var contextConnection in contextConnections)
        {
            if (contextConnection.Connection.Connection != connection.Connection)
                throw new Exception("Two different connections in the same session with the same Connection Type.");

            var relatedConnection = contextConnection.Context.Database.GetDbConnection() as SqliteConnection
                    ?? throw new NotSupportedException($"Context Connection type not supported by provider {Enum.GetName(typeof(ProviderType), _providerType)}");

            if (relatedConnection != contextConnection.Connection.Connection)
                throw new InvalidOperationException("The provided context connection are dirty. (Changes was made outside of service [Related Connection])");

            switch (command)
            {
                case Command.RemoveTransaction:

                    contextConnection.Context.Database.UseTransaction(null);

                    contextConnection.Connection = connection;

                    changedContextConnections.Add(contextConnection);

                    break;

                case Command.AddTransaction:

                    if (connection.TransactionWrapper == null)
                        throw new InvalidOperationException("The provided context transaction are dirty. (Changes was made outside of service [TransactionWrapper null])");

                    contextConnection.Context.Database.UseTransaction(connection.TransactionWrapper.Transaction);
                    contextConnection.Connection = connection;

                    changedContextConnections.Add(contextConnection);

                    break;

                case Command.SubscribeConnection:

                    contextConnection.Connection = connection;

                    changedContextConnections.Add(contextConnection);

                    break;


                case Command.SaveChangesAsync:

                    contextConnection.Connection = connection;

                    contextConnection.Context.SaveChanges();

                    changedContextConnections.Add(contextConnection);

                    break;
            }
        }

        nonChangedContextConnections.AddRange(changedContextConnections);

        var updatedSession = _sessions[sessionId] with
        {
            ContextConnections = nonChangedContextConnections
        };

        _sessions[sessionId] = updatedSession;
    }

    #endregion
}
