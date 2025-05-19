using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.External.Responses.Success.Models;

internal enum ResponseTypes
{
    Base,
    BaseDetails,
    NotMapped
}

public abstract class ConstructorWithDetails<TDetails> : Constructor where TDetails : Details, new()
{
    public virtual TDetails Details { get; init; } = new();

    public virtual object DetailsMap(IStringLocalizer resource, TDetails details)
        => details;

    public virtual object DetailsMap(IServiceProvider serviceProvider,
                                     IStringLocalizer resource,
                                     TDetails details)
        => DetailsMap(resource, details);

    public string GetKeyDetails(string text)
    {
        return $"[{Identity}].[{text}]";
    }
}

public abstract class Constructor : Common.Models.Constructor
{
    public string GetKeyTitle()
    {
        return $"[{Identity}]-[Title]";
    }
    public string GetKey()
    {
        return $"[{Identity}]";
    }

    public abstract Type Resource { get; }
    public abstract string Identity { get; }

    public virtual string[] Parameters { get; } = Array.Empty<string>();
    public virtual int Status { get; init; } = 200;
}
