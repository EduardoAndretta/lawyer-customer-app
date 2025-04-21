using LawyerCustomerApp.External.Interfaces;
using LawyerCustomerApp.External.Responses.Warning.Models;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace LawyerCustomerApp.External.Responses.Warning.Services;

internal class Service : Handler, IWarningGeneratorService
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

    public void CreateWarning(Constructor constructor)
    {
        var type = GetType(constructor);

        if (new ResponseTypes[] { ResponseTypes.Base, ResponseTypes.BaseDetails }.Contains(type))
        {
            if (type == ResponseTypes.Base)
            {
                Handle(constructor);
                return;
            }
            if (type == ResponseTypes.BaseDetails)
            {
                var result = TryExecuteWarningDetails(constructor);

                if (result)
                    return;
            }
        }

        var response = new Response()
        {
            Type = Enum.GetName(constructor.Type) ?? "Irrelevant",
            Data = new Response.DataProperties()
            {
                Title   = "NotMapped",
                Message = "NotMapped"
            }
        };

        var warningList = _dataService.GetData<List<Response>>("WarningList");

        warningList.Add(response);

        _dataService.SetData(warningList, "WarningList");
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

    private bool TryExecuteWarningDetails(Constructor constructor)
    {
        try
        {
            Type baseConstructorType = typeof(ConstructorWithDetails<>);

            Type? detailType = GetAllBaseWarningDetailsTypes(constructor.GetType(), baseConstructorType)
                .FirstOrDefault();

            if (detailType == null)
                return false;

            MethodInfo? handleDetailsMethod = GetHandleDetailsMethod(detailType);
            if (handleDetailsMethod == null)
                return false;

            handleDetailsMethod.Invoke(this, new object[] { constructor });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<Type?> GetAllBaseWarningDetailsTypes(Type? constructorType, Type baseConstructorType)
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
