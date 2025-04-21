using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Responses.Warning.Models;
using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.External.Responses.Warning.Services;

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

    public void Handle(Constructor constructor)
    {
        var warningList = _dataService.GetData<List<Response>>("WarningList");

        IStringLocalizer resource = _stringLocalizerFactory.Create(constructor.Resource);

        var key      = constructor.GetKey();
        var keyTitle = constructor.GetKeyTitle();
  
        var message = constructor.Parameters.Any() 
            ? resource[key, constructor.Parameters].Value 
            : resource[key].Value;

        var title = resource[keyTitle].Value;

        var response = new Response()
        {
            Type = Enum.GetName(constructor.Type) ?? "Irrelevant",
            Data = new()
            {
                Title   = message ?? string.Empty,
                Message = title   ?? string.Empty
            }
        };

        warningList.Add(response);

        _dataService.SetData(warningList, "WarningList");
    }

    public void Handle<TDetails>(ConstructorWithDetails<TDetails> constructor) where TDetails : Details, new()
    {
        var warningList = _dataService.GetData<List<Response>>("WarningList");

        IStringLocalizer resource = _stringLocalizerFactory.Create(constructor.Resource);

        var key      = constructor.GetKey();
        var keyTitle = constructor.GetKeyTitle();
  
        var message = constructor.Parameters.Any() 
            ? resource[key, constructor.Parameters].Value 
            : resource[key].Value;

        var title = resource[keyTitle].Value;

        object details = constructor.DetailsMap(_serviceProvider, resource, constructor.Details);

        var response = new Response()
        {
            Type = Enum.GetName(constructor.Type) ?? "Irrelevant",
            Data = new Response.DataPropertiesWithDetails()
            {
                Title   = message ?? string.Empty,
                Message = title   ?? string.Empty,
                Details = details
            }
        };

        warningList.Add(response);

        _dataService.SetData(warningList, "WarningList");
    }
}
