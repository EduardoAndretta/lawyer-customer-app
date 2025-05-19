using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Responses.Success.Models;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace LawyerCustomerApp.External.Responses.Success.Services;

internal class Service : Handler, ISuccessGeneratorService
{
    private readonly IDataService _dataService;
    public Service(
        IDataService            dataService,
        IServiceProvider        serviceProvider,
        IStringLocalizerFactory stringLocalizerFactory)
        : base(dataService, serviceProvider, stringLocalizerFactory)
    {
        _dataService = dataService;
    }

    public Response CreateSuccess(Constructor constructor)
    {
        var type = GetType(constructor);

        if (new ResponseTypes[] { ResponseTypes.Base, ResponseTypes.BaseDetails }.Contains(type))
        {
            if (type == ResponseTypes.Base)
            {
                return Handle(constructor); 
            }
            if (type == ResponseTypes.BaseDetails)
            {
                var (result, response) = TryExecuteSuccessDetails(constructor);

                if (result)
                    return response;
            }
        }

        var warningList = _dataService.GetData<List<Warning.Models.Response>>("WarningList");

        if (warningList.Any())
        {
            return new Response()
            {
                Status = "success",
                Data   = new Response.DataPropertiesWithWarning()
                {
                    Title    = "NotMapped",
                    Message  = "NotMapped",
                    Warnings = warningList
                }
            };
        }
        else
        {
            return new Response()
            {
                Status = "success",
                Data   = new Response.DataProperties()
                {
                    Title   = "NotMapped",
                    Message = "NotMapped"
                }
            };
        }
    }

    private static ResponseTypes GetType(Constructor constructor)
    {
        if (IsSubclassOfRawGeneric(typeof(ConstructorWithDetails<>), constructor.GetType()))
            return ResponseTypes.BaseDetails;

        return ResponseTypes.Base;
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        return false;
    }

    #region Details

    private (bool, Response) TryExecuteSuccessDetails(Constructor constructor)
    {
        try
        {
            Type baseConstructorType = typeof(ConstructorWithDetails<>);

            Type? detailType = GetAllBaseSuccessDetailsTypes(constructor.GetType(), baseConstructorType)
                .FirstOrDefault();

            if (detailType == null)
                return (false, new());

            MethodInfo? handleDetailsMethod = GetHandleDetailsMethod(detailType);
            if (handleDetailsMethod == null)
                return (false, new());

            if (handleDetailsMethod.Invoke(this, new object[] { constructor }) is Response responseSuccess)
                return (true, responseSuccess);

            return (false, new());
        }
        catch
        {
            return (false, new());
        }
    }

    private static IEnumerable<Type?> GetAllBaseSuccessDetailsTypes(Type? constructorType, Type baseConstructorType)
    {
        while (constructorType != null)
        {
            if (constructorType.IsGenericType && constructorType.GetGenericTypeDefinition() == baseConstructorType)
            {
                yield return constructorType.GetGenericArguments().FirstOrDefault();
            }
            constructorType = constructorType.BaseType;
        }
    }

    private MethodInfo? GetHandleDetailsMethod(Type detailType)
    {
        return typeof(Service)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == nameof(Handle)
                && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length       == 1
                && m.GetParameters()[0].ParameterType.IsGenericType
                && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ConstructorWithDetails<>))
            ?.MakeGenericMethod(detailType);
    }

    #endregion   
}
