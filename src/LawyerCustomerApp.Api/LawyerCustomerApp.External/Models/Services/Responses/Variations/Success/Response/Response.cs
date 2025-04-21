using System.Text.Json.Serialization;

namespace LawyerCustomerApp.External.Responses.Success.Visualization
{
    #region Documentation - Visualization

    public record SuccessVisualization<TData> where TData : DataProperties
    {
        public SuccessVisualization() { }
        public SuccessVisualization(string status, TData data)
        {
            Status = status;
            Data   = data;
        }
        public required string Status { get; init; }
        public required TData Data { get; init; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(DataPropertiesWithWarning))]
    [JsonDerivedType(typeof(DataPropertiesWithDetails))]
    [JsonDerivedType(typeof(DataPropertiesWithDetailsAndWarning))]
    public record DataProperties
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    // [With Warnings]
    public record DataPropertiesWithWarning : DataProperties
    {
        public IEnumerable<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>> Warnings { get; init; } = new List<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>>();
    }

    // [Details]
    public record DataPropertiesWithDetails : DataProperties
    {
        public object Details { get; init; } = new object();
    }

    // [Details with Warnings]
    public record DataPropertiesWithDetailsAndWarning : DataProperties
    {
        public object Details { get; init; } = new object();
        public IEnumerable<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>> Warnings { get; init; } = new List<Warning.Visualization.WarningVisualization<Warning.Visualization.DataProperties>>();
    }

    #endregion
}

namespace LawyerCustomerApp.External.Responses.Success.Models
{
    public record Response : Common.Models.Response
    {
        public string Status { get; init; } = string.Empty;
        public DataProperties Data { get; init; } = new();

        [JsonPolymorphic]
        [JsonDerivedType(typeof(DataPropertiesWithWarning))]
        [JsonDerivedType(typeof(DataPropertiesWithDetails))]
        [JsonDerivedType(typeof(DataPropertiesWithDetailsAndWarning))]
        public record DataProperties
        {
            public string Title { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
        }

        // [With Warnings]
        public record DataPropertiesWithWarning : DataProperties
        {
            public IEnumerable<Warning.Models.Response> Warnings { get; init; } = new List<Warning.Models.Response>();
        }

        // [Details]
        public record DataPropertiesWithDetails : DataProperties
        {
            public object Details { get; init; } = new object();
        }

        // [Details with Warnings]
        public record DataPropertiesWithDetailsAndWarning : DataProperties
        {
            public object Details { get; init; } = new object();
            public IEnumerable<Warning.Models.Response> Warnings { get; init; } = new List<Warning.Models.Response>();
        }
    }
}
