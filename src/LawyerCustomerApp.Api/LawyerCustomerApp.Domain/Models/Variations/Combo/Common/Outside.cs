using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LawyerCustomerApp.Domain.Combo.Common.Models;

public record KeyValueParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }
    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int Begin { get; init; }
        public required int End { get; init; }
    }
}

public record KeyValueParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public KeyValueParameters ToOrdinary()
    {
        return new KeyValueParameters
        {
            UserId = this.UserId ?? 0,
            RoleId = this.RoleId ?? 0,

            Pagination = new() 
            {
                Begin = this.Pagination?.Begin ?? 0,
                End   = this.Pagination?.End   ?? 0
            }
        };
    }

    public class PaginationProperties
    {
        public int? Begin { get; init; }
        public int? End { get; init; }
    }
}

public record KeyValueInformation<TValue>
{
    public required IEnumerable<Item<TValue>> Items { get; init; }
    public class Item<T>
    {
        public required string Key { get; init; }
        public T? Value { get; init; }
    }

    public KeyValueInformationDto<TValue> ToDto()
    {
        return new KeyValueInformationDto<TValue>
        {
            Items = this.Items.Select(x =>
                new KeyValueInformationDto<TValue>.Item<TValue>
                {
                    Key   = x.Key,
                    Value = x.Value,
                })
        };     
    }
}

public record KeyValueInformationDto<TValue>
{
    public IEnumerable<Item<TValue>> Items { get; init; } = new Collection<Item<TValue>>();
    public class Item<T>
    {
        public required string Key { get; init; }
        public T? Value { get; init; }
    }
}