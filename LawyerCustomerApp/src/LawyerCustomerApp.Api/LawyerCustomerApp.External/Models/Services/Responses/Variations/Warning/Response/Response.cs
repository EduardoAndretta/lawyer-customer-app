using System.Text.Json.Serialization;

namespace LawyerCustomerApp.External.Responses.Warning.Visualization
{
    #region Documentation - Visualization

    public record WarningVisualization<TData> where TData : DataProperties
    {
        public required string Type { get; init; }
        public required TData Data { get; init; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(DataPropertiesWithDetails))]
    public record DataProperties
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    // [Details]
    public record DataPropertiesWithDetails : DataProperties
    {
        public object Details { get; init; } = new object();
    }

    #endregion
}

namespace LawyerCustomerApp.External.Responses.Warning.Models
{
    public record Response : Common.Models.Response
    {
        public string Type { get; init; } = string.Empty;
        public DataProperties Data { get; init; } = new();


        [JsonPolymorphic]
        [JsonDerivedType(typeof(DataPropertiesWithDetails))]
        public record DataProperties
        {
            public string Title { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
        }

        // [Details]
        public record DataPropertiesWithDetails : DataProperties
        {
            public object Details { get; init; } = new object();
        }
    }
}