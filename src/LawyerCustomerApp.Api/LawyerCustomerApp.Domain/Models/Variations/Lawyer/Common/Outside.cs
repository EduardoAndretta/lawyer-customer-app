using System.Text.Json.Serialization;

namespace LawyerCustomerApp.Domain.Lawyer.Common.Models;

public record SearchParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

    public string? Query { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public SearchParameters ToOrdinary()
    {
        return new SearchParameters
        {
            Query = this.Query ?? string.Empty,

            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,

            Pagination = new() 
            {
                Begin = this.Pagination?.Begin ?? 0,
                End   = this.Pagination?.End   ?? 0
            },
        };
    }

    public class PaginationProperties
    {
        public int? Begin { get; init; }
        public int? End { get; init; }
    }
}

public class SearchParameters
{
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required string Query { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int Begin { get; init; }
        public required int End { get; init; }
    }
}

public record DetailsParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? LawyerId { get; init; }
    public int? AttributeId { get; init; }

    public DetailsParameters ToOrdinary()
    {
        return new DetailsParameters
        {
            UserId      = this.UserId      ?? 0,
            LawyerId    = this.LawyerId    ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0
        };
    }
}

public class DetailsParameters
{
    public required int UserId { get; init; }
    public required int LawyerId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }
}

public record RegisterParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public string? Phone { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {
            UserId = this.UserId ?? 0,
            RoleId = this.RoleId ?? 0,

            Phone = this.Phone ?? string.Empty,
        };
    }
}

public class RegisterParameters
{
    public required int UserId { get; init; } = 0;
    public required int RoleId { get; init; } = 0;

    public required string Phone { get; init; } = string.Empty;

    public RegisterParametersDto ToDto()
    {
        return new RegisterParametersDto
        {
            UserId = this.UserId,
            Phone  = this.Phone,
        };
    }
}

public record SearchInformationDto
{
    public IEnumerable<ItemProperties>? Items { get; init; }

    public class ItemProperties
    {
        public string? Name { get; init; }
      
        public int? UserId { get; init; }
        public int? LawyerId { get; init; }
    }
}

public record SearchInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string Name { get; init; }

        public required int UserId { get; init; }
        public required int LawyerId { get; init; }
    }

    public SearchInformationDto ToOrdinary()
    {
        return new SearchInformationDto
        {
            Items = this.Items.Select(x =>
                new SearchInformationDto.ItemProperties
                {
                    Name = x.Name,

                    UserId   = x.UserId,
                    LawyerId = x.LawyerId
                })
        };
    }
}

public record CountParametersDto
{
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public string? Query { get; init; }

    public CountParameters ToOrdinary()
    {
        return new CountParameters
        {
            Query = this.Query ?? string.Empty,

            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0
        };
    }
}

public class CountParameters
{
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required string Query { get; init; }
}



public record DetailsInformationDto
{
    public ItemProperties? Item { get; init; }

    public class ItemProperties
    {
        public string? Name { get; init; }
      
        public int? UserId { get; init; }
        public int? LawyerId { get; init; }
    }
}

public record CountInformation
{
    public required long Count { get; init; }

    public CountInformationDto ToOrdinary()
    {
        return new CountInformationDto
        {
            Count = this.Count
        };
    }
}

public record CountInformationDto
{
    public long? Count { get; init; }
}

public record DetailsInformation
{
    public required ItemProperties Item { get; init; }

    public class ItemProperties
    {
        public required string Name { get; init; }

        public required int UserId { get; init; }
        public required int LawyerId { get; init; }
    }

    public DetailsInformationDto ToOrdinary()
    {
        return new DetailsInformationDto
        {
            Item = new()
            {
                Name = this.Item.Name,

                UserId   = this.Item.UserId,
                LawyerId = this.Item.LawyerId
            }
        };
    }
}