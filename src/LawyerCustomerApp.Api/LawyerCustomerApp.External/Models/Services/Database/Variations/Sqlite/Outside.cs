using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Transactions;

namespace LawyerCustomerApp.External.Database.Sqlite.Models;

public abstract class ConnectionWrapper : Common.Models.ConnectionWrapper
{
    public ConnectionWrapper(SqliteConnection connection, Guid sessionId) : base(connection, sessionId)
    {
        Connection = connection;
    }

    public new SqliteConnection Connection { get; init; }

    public TransactionWrapper? TransactionWrapper { get; set; }

    public override Common.Models.ProviderType ProviderType => Common.Models.ProviderType.Sqlite;

    public void SetTransaction(TransactionWrapper transactionWrapper)
    {
        Transaction = transactionWrapper.Transaction;
        TransactionWrapper = transactionWrapper;
    }

    public void RemoveTransaction()
    {
        Transaction = null;
        TransactionWrapper = null;
    }

}

public class CustomConnection : ConnectionWrapper
{
    public CustomConnection(SqliteConnection connection, Guid sessionId, string connectionType) : base(connection, sessionId)
    {
        ConnectionType = connectionType;
    }

    public string ConnectionType { get; init; }
}


public class TransactionWrapper
{
    public SqliteTransaction Transaction { get; internal set; }

    private bool _isTransactionDead;
    public bool IsTransactionDead => _isTransactionDead;

    public TransactionWrapper(SqliteTransaction transaction)
    {
        Transaction = transaction;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isTransactionDead)
        {
            _isTransactionDead = true;
        }

        await Transaction.DisposeAsync();
    }

    public void Dispose()
    {
        if (!_isTransactionDead)
        {
            _isTransactionDead = true;
        }

        Transaction.Dispose();
    }

    public async ValueTask RollbackAsync()
    {
        await Transaction.RollbackAsync();
    }

    public async ValueTask CommitAsync()
    {
        await Transaction.CommitAsync();
    }
}


public class ContextConnectionWrapper : Common.Models.ContextConnectionWrapper
{
    public ContextConnectionWrapper(DbContext context, ConnectionWrapper connection, Type contextType, Guid sessionId) : base(context, connection, contextType, sessionId)
    {
    }
}
