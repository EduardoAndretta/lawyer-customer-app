using LawyerCustomerApp.External.Responses.Error.Models;
using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.External.Common.Responses.Error;

internal class Error { }

internal class ValidationError : ConstructorWithDetails<ValidationError.DetailsVariation>
{
    public override string Identity => "ValidationError";
    public override Type Resource => typeof(Error);

    public override object DetailsMap(IStringLocalizer resource, DetailsVariation details)
    {
        return details.Errors.Select(x =>
        {
            var key = GetKeyDetails(x.Identity);

            return new
            {
                Field   = x.Field,
                Message = x.Parameters.Any()
                    ? resource[key, x.Parameters].Value
                    : resource[key].Value
            };
        });
    }

    // [Parameters]
    public required string SourceCode { get; init; }
    public override string[] Parameters => new string[] { SourceCode };

    public record DetailsVariation : Details
    {
        public IEnumerable<Item> Errors { get; init; } = new List<Item>();
        public record Item
        {
            public string Field { get; init; } = string.Empty;
            public string Identity { get; init; } = string.Empty;
            public string[] Parameters { get; init; } = Array.Empty<string>();
        }
    }
}

public class ModelStateError : Constructor
{
    public override string Identity => "ModelStateError";
    public override Type Resource => typeof(Error);

    // [Parameters]
    public required string SourceCode { get; init; }
    public required string Errors { get; init; }

    public override string[] Parameters => new string[] { SourceCode, Errors };
}

public class NotFoundDatabaseConnectionStringError : Constructor
{
    public override string Identity => "NotFoundDatabaseConnectionStringError";
    public override Type Resource => typeof(Error);
}


public class DatabaseConnectionError : Constructor
{
    public override string Identity => "DatabaseConnectionError";
    public override Type Resource => typeof(Error);
}
