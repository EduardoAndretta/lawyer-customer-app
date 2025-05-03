using LawyerCustomerApp.External.Responses.Error.Models;
using Microsoft.Extensions.Localization;

namespace LawyerCustomerApp.Domain.Common.Responses.Error;

internal class Error { }


public class MultipleValidationError : ConstructorWithDetails<MultipleValidationError.DetailsVariation>
{
    public override string Identity => "ValidationError";
    public override Type Resource => typeof(Error);

    public override object DetailsMap(IStringLocalizer resource, DetailsVariation details)
    {
        const int MaxDepth = 30;

        object HandleNestedErrors(DetailsVariation.Item item, int depth = 0)
        {
            if (depth > MaxDepth)
                throw new Exception("Max recursion depth reached.");

            var key = GetKeyDetails(item.Identity);

            if (item is not DetailsVariation.ItemWithErrors itemWithErrors)
                return new
                {
                    Field   = item.Field,
                    Message = item.Parameters.Any()
                        ? resource[key, item.Parameters].Value
                        : resource[key].Value
                };

            if (!details.Errors.Any())
            {
                return new
                {
                    Key     = itemWithErrors.Key,
                    Field   = itemWithErrors.Field,
                    Message = itemWithErrors.Parameters.Any()
                        ? resource[key, itemWithErrors.Parameters].Value
                        : resource[key].Value
                };
            }

            return new
            {
                Key     = itemWithErrors.Key,
                Field   = itemWithErrors.Field,
                Message = itemWithErrors.Parameters.Any()
                    ? resource[key, itemWithErrors.Parameters].Value
                    : resource[key].Value,

                Errors = itemWithErrors.Errors.Select(x => HandleNestedErrors(x, depth + 1))
            };
        }

        return details.Errors.Select(x => HandleNestedErrors(x));
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

        public record ItemWithErrors : Item
        {
            public string Key { get; init; } = string.Empty;
            public IEnumerable<Item> Errors { get; init; } = new List<Item>();
        }
    }

    public override int Status => 400;
}


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

    public override int Status => 400;
}

public class ModelStateError : Constructor
{
    public override string Identity => "ModelStateError";
    public override Type Resource => typeof(Error);

    // [Parameters]
    public required string SourceCode { get; init; }
    public required string Errors { get; init; }

    public override string[] Parameters => new string[] { SourceCode, Errors };

    public override int Status => 400;
}

public class NotFoundDatabaseConnectionStringError : Constructor
{
    public override string Identity => "NotFoundDatabaseConnectionStringError";
    public override Type Resource => typeof(Error);

    public override int Status => 500;
}


public class DatabaseConnectionError : Constructor
{
    public override string Identity => "DatabaseConnectionError";
    public override Type Resource => typeof(Error);

    public override int Status => 500;
}
