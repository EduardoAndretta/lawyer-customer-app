using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Models.Context;

namespace LawyerCustomerApp.External.Interfaces;

public interface IInitializerService
{    
    /// <summary>
    /// Initialize the Database. [Sqlite]
    /// </summary>
    Task<Result> InitializeSqliteDatabase(Contextualizer contextualizer);
}
