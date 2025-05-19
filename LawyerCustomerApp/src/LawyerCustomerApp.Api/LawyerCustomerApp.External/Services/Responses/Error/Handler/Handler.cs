using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Responses.Error.Models;
using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.External.Responses.Error.Services;

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

        var response =
            new Response()
            {
                Status = "error",
                Error  = warningList.Any() 
                    ? new Response.ErrorPropertiesWithWarning()
                    {
                        StatusNumber = constructor.Status,
                        Title        = title   ?? string.Empty,
                        Message      = message ?? string.Empty,
                        Warnings     = warningList
                    }
                    : new Response.ErrorProperties()
                    {
                        StatusNumber = constructor.Status,
                        Title        = title   ?? string.Empty,
                        Message      = message ?? string.Empty
                    }  
            };

        return response;
    }


    public Response Handle<TDetails>(ConstructorWithDetails<TDetails> constructor) 
        where TDetails : Details, new()
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

        var response =
           new Response()
           {
               Status = "error",
               Error  = warningList.Any() 
                   ? new Response.ErrorPropertiesWithDetailsAndWarning()
                   {
                       StatusNumber = constructor.Status,
                       Title        = title   ?? string.Empty,
                       Message      = message ?? string.Empty,
                       Warnings     = warningList,
                       Details      = details
                   }
                   : new Response.ErrorPropertiesWithDetails()
                   {
                       StatusNumber = constructor.Status,
                       Title        = title   ?? string.Empty,
                       Message      = message ?? string.Empty,
                       Details      = details
                   }  
           };

        return response;
    }
}
