using Dapper;
using LawyerCustomerApp.External.Database.Common.Models;
using LawyerCustomerApp.External.Extensions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Models.Context;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using System.Text;
using System.Threading;

namespace LawyerCustomerApp.External.Permission.Repositories;

internal class Repository
{
    private readonly IDatabaseService _databaseService;
    public Repository(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    
}
