using LawyerCustomerApp.External.Exceptions;
using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Responses.Error.Models;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace LawyerCustomerApp.External.Responses.Error.Services;

internal class Service : Handler, IErrorGeneratorService
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

    public Response CreateError(Constructor constructor)
    {
        var type = GetType(constructor);

        if (new ResponseTypes[] { ResponseTypes.Base, ResponseTypes.BaseDetails }.Contains(type))
        {
            if (type == ResponseTypes.Base)
            {
                if (constructor is Constructor parsedConstructor)
                {
                    return Handle(parsedConstructor);
                }
            }

            if (type == ResponseTypes.BaseDetails)
            {
                if (TryExecuteExceptionDetails(constructor, out var response))
                {
                    return response;
                }
            }
        }
        
        var warningList = _dataService.GetData<List<Warning.Models.Response>>("WarningList");

        var responseError = new Response()
        {
            Status = "error",
            Error  = warningList.Any() 
                ? new Response.ErrorPropertiesWithWarning()
                {
                    StatusNumber = 500,
                    Title        = "Exception",
                    Message      = constructor?.Identity ?? "Empty",
                    Warnings     = warningList
                }
                : new Response.ErrorProperties()
                {
                    StatusNumber = 500,
                    Title        = "Exception",
                    Message      = constructor?.Identity ?? "Empty"
                }
        };

        return responseError;
    }

    public Response CreateError(Exception exception)
    {
        if (IsBaseExceptionWithConstructor(exception, out var constructor))
        {
            var type = GetType(constructor!);

            if (new ResponseTypes[] { ResponseTypes.Base, ResponseTypes.BaseDetails }.Contains(type))
            {
                if (type == ResponseTypes.Base)
                {
                    if (constructor is Constructor parsedConstructor)
                    {
                        return Handle(parsedConstructor);
                    }
                }

                if (type == ResponseTypes.BaseDetails)
                {
                    if (TryExecuteExceptionDetails(constructor!, out var response))
                    {
                        return response;
                    }
                }
            }
        }

        var warningList = _dataService.GetData<List<Warning.Models.Response>>("WarningList");

        var detailsError = new
        {
            InnerExceptionMessage = exception?.InnerException?.Message                                 ?? "Empty",
            Source                = exception?.Source                                                  ?? "Empty",
            Target                = exception?.TargetSite?.ToString()                                  ?? "Empty",
            StackTrace            = exception?.StackTrace?.Split(Environment.NewLine).FirstOrDefault() ?? "Empty"
        };

        var responseError = new Response()
        {
            Status = "error",
            Error  = warningList.Any() 
                ? new Response.ErrorPropertiesWithDetailsAndWarning()
                {
                    StatusNumber = 500,
                    Title        = "Exception",
                    Message      = exception?.Message ?? "Empty",
                    Warnings     = warningList,
                    Details      = detailsError
                }
                : new Response.ErrorPropertiesWithDetails()
                {
                    StatusNumber = 500,
                    Title        = "Exception",
                    Message      = exception?.Message ?? "Empty",
                    Details      = detailsError
                }
        };  

        return responseError;
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

    private bool TryExecuteExceptionDetails(Constructor costructor, out Response responseError)
    {
        responseError = new Response()
        { 
            Status = string.Empty,
            Error  = new() 
            { 
                Message      = string.Empty,
                Title        = string.Empty,
                StatusNumber = 0,
            },
        };

        try
        {     
            Type baseConstructorDetailsType = typeof(ConstructorWithDetails<>);

            Type? detailsType = GetAllBaseErrorDetailsTypes(costructor.GetType(), baseConstructorDetailsType)
                .FirstOrDefault();

            if (detailsType == null)
                return false;

            MethodInfo? handleDetailsMethod = GetHandleDetailsMethod(detailsType);
            if (handleDetailsMethod == null)
                return false;

            if (handleDetailsMethod.Invoke(this, new object[] { costructor }) is Response existingSesponseError)
            {
                responseError = existingSesponseError;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<Type?> GetAllBaseErrorDetailsTypes(Type? constructorType, Type baseConstructorDetailsType)
    {
        while (constructorType != null)
        {
            if (constructorType.IsGenericType && constructorType.GetGenericTypeDefinition() == baseConstructorDetailsType)
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

    #region Constructor - Base Exception

    private static bool IsBaseExceptionWithConstructor(Exception exception, out Constructor? constructor)
    {
        constructor = default;

        var baseExceptionType = exception.GetType();
        if (!baseExceptionType.IsGenericType || baseExceptionType.GetGenericTypeDefinition() != typeof(BaseException<>))
            return false;

        var genericArgumentType = baseExceptionType.GetGenericArguments()[0];
        if (!typeof(Constructor).IsAssignableFrom(genericArgumentType))
            return false;

        var constructorProperty = baseExceptionType.GetProperty("Constructor");
        if (constructorProperty?.GetValue(exception) is not Constructor parsedConstructor)
            return false;

        constructor = parsedConstructor;
        return true;
    }

    #endregion
}
