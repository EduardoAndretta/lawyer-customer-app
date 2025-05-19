using System.Data;

namespace LawyerCustomerApp.External.Database.Common.Models;

public class TransactionOptions
{
    public bool ExecuteRollbackAndCommit { get; init; } = false;
    public IsolationLevel IsolationLevel { get; init; } = IsolationLevel.ReadCommitted;
}
