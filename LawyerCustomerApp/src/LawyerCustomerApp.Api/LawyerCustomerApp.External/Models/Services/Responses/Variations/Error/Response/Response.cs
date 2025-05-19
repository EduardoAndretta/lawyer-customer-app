using System.Text.Json.Serialization;

namespace LawyerCustomerApp.External.Responses.Error.Visualization
{
    #region Documentation - Visualization

    public record ErrorVisualization<TError> where TError : ErrorProperties
    {
        public required string Status { get; init; }
        public required TError Error { get; init; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(ErrorPropertiesWithWarning))]
    [JsonDerivedType(typeof(ErrorPropertiesWithDetails))]
    [JsonDerivedType(typeof(ErrorPropertiesWithDetailsAndWarning))]
    public record ErrorProperties
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    // [With Warnings]
    public record ErrorPropertiesWithWarning : ErrorProperties
    {
        public IEnumerable<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>> Warnings { get; init; } = new List<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>>();
    }

    // [Details]
    public record ErrorPropertiesWithDetails : ErrorProperties
    {
        public object Details { get; init; } = new object();
    }

    // [Details with Warnings]
    public record ErrorPropertiesWithDetailsAndWarning : ErrorProperties
    {
        public object Details { get; init; } = new object();
        public IEnumerable<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>> Warnings { get; init; } = new List<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>>();
    }

    #endregion
}

namespace LawyerCustomerApp.External.Responses.Error.Models
{
    public record Response : Common.Models.Response
    {
        public required string Status { get; init; }
        public required ErrorProperties Error { get; init; }

        [JsonPolymorphic]
        [JsonDerivedType(typeof(ErrorPropertiesWithWarning))]
        [JsonDerivedType(typeof(ErrorPropertiesWithDetails))]
        [JsonDerivedType(typeof(ErrorPropertiesWithDetailsAndWarning))]
        public record ErrorProperties
        {
            public required int StatusNumber { get; init; }
            public required string Title { get; init; }
            public required string Message { get; init; }
        }

        public record ErrorPropertiesWithWarning : ErrorProperties
        {
            public required IEnumerable<Warning.Models.Response> Warnings { get; init; }
        }

        public record ErrorPropertiesWithDetails : ErrorProperties
        {
            public required object Details { get; init; }
        }

        public record ErrorPropertiesWithDetailsAndWarning : ErrorProperties
        {
            public required IEnumerable<Warning.Models.Response> Warnings { get; init; }
            public required object Details { get; init; }
        }
    } 
}
