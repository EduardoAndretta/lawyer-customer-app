using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Interfaces;
using Microsoft.EntityFrameworkCore;

using Sqlite = LawyerCustomerApp.External.Database.Sqlite.Models;

namespace LawyerCustomerApp.External.Models.Context;

public class ConnectionContextualizer
{
    private class InternalConnection
    {
        public required ProviderType ProviderType { get; init; }

        public required Guid SessionId { get; init; }

        public class Custom : InternalConnection
        {
            public required string ConnectionType { get; init; }
        }
    }

    private InternalConnection Connection { get; init; }
    private HashSet<InternalConnection> ExtraConnections { get; init; } = new();

    public ConnectionContextualizer(ConnectionWrapper connection)
    {
        switch (connection.ProviderType)
        {
            case ProviderType.Sqlite:

                Connection = connection switch
                {
                    Sqlite.CustomConnection variation => new InternalConnection.Custom 
                    { 
                        ConnectionType = variation.ConnectionType, 
                        ProviderType   = variation.ProviderType,
                        SessionId      = variation.SessionId
                    },

                    _ => throw new NotImplementedException()
                };

                break;

            default:
                throw new NotSupportedException("Connection Wrapper not supported");
        }
    }

    public void AppendExtraConnection(ConnectionWrapper connection)
    {
        switch (connection.ProviderType)
        {
            case ProviderType.Sqlite:

                ExtraConnections.Add(
                    connection switch
                    {
                        Sqlite.CustomConnection variation => new InternalConnection.Custom 
                        { 
                            ConnectionType = variation.ConnectionType, 
                            ProviderType   = variation.ProviderType,
                            SessionId      = variation.SessionId
                        },

                        _ => throw new NotImplementedException()
                    });

                break;

            default:
                throw new NotSupportedException("Connection Wrapper not supported");
        }
    }

    public async Task<ConnectionWrapper> GetConnection(IDatabaseService service, ProviderType allowedProviderTypes)
    {
        if (Connection.ProviderType != allowedProviderTypes)
            throw new Exception($"This provider isn't allowed to the current scenario. {Enum.GetName(allowedProviderTypes)}");

        return Connection switch
        {
            InternalConnection.Custom variation => await service.GetConnection(variation.ConnectionType, variation.SessionId, variation.ProviderType),
            _ => throw new NotImplementedException()
        };
    }

    public async Task<ContextConnectionWrapper> GetContext<TContext>(IDatabaseService service, ProviderType allowedProviderTypes) where TContext : DbContext
    {
        if (Connection.ProviderType != allowedProviderTypes)
            throw new Exception($"This provider isn't allowed to the current scenario. {Enum.GetName(allowedProviderTypes)}");

        return Connection switch
        {
            InternalConnection.Custom variation => await service.GetContext<TContext>(variation.ConnectionType, variation.SessionId, variation.ProviderType),
            _ => throw new NotImplementedException()
        };
    }

    private async Task<ConnectionWrapper> FindExtraConnection<TConnectionType>(
        IDatabaseService service,
        Guid             sessionId,
        TConnectionType  connectionType,
        ProviderType     providerType,
        Func<InternalConnection, TConnectionType> getConnectionType)
    {
        var internalConnection = ExtraConnections.FirstOrDefault(x =>
             x.SessionId    == sessionId    &&
             x.ProviderType == providerType &&
             EqualityComparer<TConnectionType>.Default.Equals(getConnectionType(x), connectionType))
             ?? 
             throw new Exception($"No connection of provider {Enum.GetName(providerType)} found for session {sessionId} with connection type {connectionType}");

        return Connection switch
        {
            InternalConnection.Custom variation => await service.GetConnection(variation.ConnectionType, variation.SessionId, variation.ProviderType),
            _ => throw new NotImplementedException()
        };
    }

    private async Task<ContextConnectionWrapper> FindExtraContext<TConnectionType, TContext>(
        IDatabaseService service,
        Guid             sessionId,
        TConnectionType  connectionType,
        ProviderType     providerType,
        Func<InternalConnection, TConnectionType> getConnectionType) where TContext : DbContext
    {
        var internalConnection = ExtraConnections.FirstOrDefault(x =>
             x.SessionId    == sessionId    &&
             x.ProviderType == providerType &&
             EqualityComparer<TConnectionType>.Default.Equals(getConnectionType(x), connectionType))
             ?? 
             throw new Exception($"No connection of provider {Enum.GetName(providerType)} found for session {sessionId} with connection type {connectionType}");

        return Connection switch
        {
            InternalConnection.Custom variation => await service.GetContext<TContext>(variation.ConnectionType, variation.SessionId, variation.ProviderType),
            _ => throw new NotImplementedException()
        };
    }

    public async Task<ConnectionWrapper> GetExtraConnection(
        IDatabaseService repository,
        Guid             sessionId, 
        string           connectionType, 
        ProviderType     providerType)
    {
        switch (providerType)
        {
            case ProviderType.Sqlite:

                return await FindExtraConnection<string?>(
                    repository,
                    sessionId,
                    connectionType,
                    providerType,
                    x =>
                    {
                        if (x is InternalConnection.Custom variation)
                            return variation.ConnectionType;
                        return null;
                    });

            default:
                throw new NotSupportedException("Connection Wrapper not supported");
        } 
    }

    public async Task<ContextConnectionWrapper> GetExtraConnection<TContext>(
        IDatabaseService service,
        Guid             sessionId,
        string           connectionType,
        ProviderType     providerType) where TContext : DbContext
    {
        switch (providerType)
        {
            case ProviderType.Sqlite:

                return await FindExtraContext<string?, TContext>(
                    service,
                    sessionId,
                    connectionType,
                    providerType,
                    x =>
                    {
                        if (x is InternalConnection.Custom variation)
                            return variation.ConnectionType;
                        return null;
                    });

            default:
                throw new NotSupportedException("Connection Wrapper not supported");
        } 
    }
}
