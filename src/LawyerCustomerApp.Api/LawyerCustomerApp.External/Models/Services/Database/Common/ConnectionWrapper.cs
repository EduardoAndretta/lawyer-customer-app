using Microsoft.EntityFrameworkCore;
using System.Data;

namespace LawyerCustomerApp.External.Database.Common.Models;

public enum ProviderType
{
    Sqlite
}

public abstract class ConnectionWrapper
{
    public ConnectionWrapper(IDbConnection connection, Guid sessionId)
    {
        Connection = connection;
        SessionId  = sessionId;
    }

    public ValueTask<Guid> GetConnectionId()
    {
        return ValueTask.FromResult(ConnectionIdentifier);
    }

    public IDbConnection Connection { get; init; }
    public IDbTransaction? Transaction { get; set; }

    public bool Current { get; set; } = false;
    public Guid SessionId { get; init; }

    public int HashCode => Connection.GetHashCode();

    public readonly Guid ConnectionIdentifier = Guid.NewGuid();

    public abstract ProviderType ProviderType { get; }
}

public abstract class ContextConnectionWrapper
{
    internal ContextConnectionWrapper(DbContext context, ConnectionWrapper connection, Type contextType, Guid sessionId)
    {
        Context     = context;
        ContextType = contextType;
        Connection  = connection;
        SessionId   = sessionId;
    }

    public DbContext Context { get; init; }
    public ConnectionWrapper Connection { get; set; }

    public Type ContextType { get; init; }
    public Guid SessionId { get; init; }

    public int HashCode => Context.GetHashCode();

    public readonly Guid ConnectionIdentifier = Guid.NewGuid();

    public TContext GetContext<TContext>() where TContext : DbContext
    {
        if (typeof(TContext) != ContextType)
            throw new Exception($"Invalid Context Type. Correct Type is {ContextType.FullName}");

        return (TContext)Context;
    }
}
