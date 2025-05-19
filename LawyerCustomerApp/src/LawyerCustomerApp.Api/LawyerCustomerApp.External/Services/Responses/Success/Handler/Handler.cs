using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Responses.Success.Models;
using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.External.Responses.Success.Services;

internal class Handler
{
    private readonly IDataService            _dataService;
    private readonly IServiceProvider        _serviceProvider;
    private readonly IStringLocalizerFactory _stringLocalizerFactory;
    public Handler(
        IDataService            dataService,
        IServiceProvider        serviceProvider,
        IStringLocalizerFactory stringLocalizerFactory)
    {
        _dataService            = dataService;
        _serviceProvider        = serviceProvider;
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    public Response Handle<TDetails>(ConstructorWithDetails<TDetails> constructor) where TDetails : Details, new()
    {
        var warningList = _dataService.GetData<List<Warning.Models.Response>>("WarningList");

        IStringLocalizer resource = _stringLocalizerFactory.Create(constructor.Resource); 

        var key      = constructor.GetKey();
        var keyTitle = constructor.GetKeyTitle();

        var message = constructor.Parameters.Any() 
            ? resource[key, constructor.Parameters].Value 
            : resource[key].Value;

        var title = resource[keyTitle].Value;

        object details = constructor.DetailsMap(_serviceProvider, resource, constructor.Details);

        var response =  warningList.Any()
            ? new Response()
                {
                    Status = "success",
                    Data   = new Response.DataPropertiesWithDetailsAndWarning()
                    {
                        Title    = title   ?? string.Empty,
                        Message  = message ?? string.Empty,
                        Details  = details,
                        Warnings = warningList
                    }
            }
            : new Response()
                {
                    Status = "success",
                    Data   = new Response.DataPropertiesWithDetails()
                    {
                        Title   = title   ?? string.Empty,
                        Message = message ?? string.Empty,
                        Details = details,
                    }
                };

        return response;
    }

    public Response Handle(Constructor constructor)
    {
        var warningList = _dataService.GetData<List<Warning.Models.Response>>("WarningList");

        IStringLocalizer resource = _stringLocalizerFactory.Create(constructor.Resource);

        var key      = constructor.GetKey();
        var keyTitle = constructor.GetKeyTitle();

        var message = constructor.Parameters.Any()
            ? resource[key, constructor.Parameters].Value
            : resource[key].Value;

        var title = resource[keyTitle].Value;

        var response = warningList.Any()
            ? new Response()
                {
                    Status = "success",
                    Data   = new Response.DataPropertiesWithWarning()
                    {
                        Title    = title   ?? string.Empty,
                        Message  = message ?? string.Empty,
                        Warnings = warningList
                    }
                }
            : new Response()
                {
                    Status = "success",
                    Data   = new Response.DataProperties()
                    {
                        Title   = title   ?? string.Empty,
                        Message = message ?? string.Empty,
                    }
                };

        return response;
    }
}
