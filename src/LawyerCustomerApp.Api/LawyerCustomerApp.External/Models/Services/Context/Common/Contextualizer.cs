using LawyerCustomerApp.External.Database.Common.Models;
using System.Threading;

namespace LawyerCustomerApp.External.Models.Context;

public record Contextualizer
{
    private CancellationToken _cancellationToken;
    public CancellationToken CancellationToken => _cancellationToken;

    private ConnectionContextualizer? _connectionContextualizer;
    public ConnectionContextualizer ConnectionContextualizer
    {
        get => _connectionContextualizer
            ?? throw new InvalidOperationException("This ConnectionContextualizer was not assigned by 'AssignContextualizedConnection' before");
    }

    private Contextualizer(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    private Contextualizer(CancellationToken cancellationToken, ConnectionContextualizer connectionContextualizer)
    {
        _cancellationToken        = cancellationToken;
        _connectionContextualizer = connectionContextualizer;
    }

    public static Contextualizer Init(CancellationToken cancellationToken)
    {
        return new Contextualizer(
            cancellationToken: cancellationToken
        );
    }

    public void AssignContextualizedConnection(ConnectionWrapper connection)
    {
        _connectionContextualizer = new ConnectionContextualizer(connection);
    }

    public void AssignContextualizedConnection(ContextConnectionWrapper connection)
    {
        _connectionContextualizer = new ConnectionContextualizer(connection.Connection);
    }
}
