using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LawyerCustomerApp.Domain.Case.Common.Models;

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
            }
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


public record CountParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

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


public record DetailsParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? CaseId { get; init; }
    public int? AttributeId { get; init; }

    public DetailsParameters ToOrdinary()
    {
        return new DetailsParameters
        {
            UserId        = this.UserId      ?? 0,
            CaseId        = this.CaseId      ?? 0,
            AttributeId   = this.AttributeId ?? 0,
            RoleId        = this.RoleId      ?? 0
        };
    }
}

public class DetailsParameters
{
    public required int UserId { get; init; }
    public required int CaseId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }
}

public record RegisterParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

    public string? Title { get; init; }
    public string? Description { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {
            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,


            Title       = this.Title       ?? string.Empty,
            Description = this.Description ?? string.Empty

        };
    }
}

public class RegisterParameters
{
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required string Title { get; init; }
    public required string Description { get; init; }
}

public record AssignLawyerParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? CaseId { get; init; }
    public int? AttributeId { get; init; }

    public int? LawyerId { get; init; }
   
    public AssignLawyerParameters ToOrdinary()
    {
        return new AssignLawyerParameters
        {
            CaseId      = this.CaseId      ?? 0,
            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,

            LawyerId = this.LawyerId ?? 0
        };
    }
}

public class AssignLawyerParameters
{
    public required int CaseId { get; init; }
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required int LawyerId { get; init; }

}

public record AssignCustomerParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? CaseId { get; init; }
    public int? AttributeId { get; init; }

    public int? CustomerId { get; init; }

    public AssignCustomerParameters ToOrdinary()
    {
        return new AssignCustomerParameters
        {
            CaseId      = this.CaseId      ?? 0,
            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,

            CustomerId = this.CustomerId ?? 0
        };
    }
}

public class AssignCustomerParameters
{
    public required int CaseId { get; init; }
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required int CustomerId { get; init; }
}


public record EditParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }
    
    public int? AttributeId { get; init; }

    public int? RelatedCaseId { get; init; }

    public object? Values { get; init; }

    public EditParameters ToOrdinary()
    {
        static EditParameters.PatchField<string?> TryParseToString(JsonNode? node)
        {
            if (node == null)
                return new EditParameters.PatchField<string?>();

            var valueKind = node.GetValueKind();

            if (valueKind != JsonValueKind.String)
                return new EditParameters.PatchField<string?>();

            var value = node.GetValue<object>();

            return new EditParameters.PatchField<string?>()
            {
                Received = true,
                Value    = node.GetValue<string?>()
            };
        }

        static EditParameters.PatchField<bool?> TryParseToBoolean(JsonNode? node)
        {
            if (node == null)
                return new EditParameters.PatchField<bool?>();

            var valueKind = node.GetValueKind();

            if (valueKind != JsonValueKind.True && valueKind != JsonValueKind.False)
                return new EditParameters.PatchField<bool?>();

            return new EditParameters.PatchField<bool?>()
            {
                Received = true,
                Value    = node.GetValue<bool?>()
            };
        }

        var jsonNode = Values as JsonNode;

        if (jsonNode == null && Values is JsonElement el)
            jsonNode = JsonNode.Parse(el.GetRawText());

        if (jsonNode == null)
        {
            return new EditParameters
            {
                RelatedCaseId = this.RelatedCaseId ?? 0,

                UserId      = this.UserId      ?? 0,
                RoleId      = this.RoleId      ?? 0,
                AttributeId = this.AttributeId ?? 0
            };
        }

        return new EditParameters
        {
            RelatedCaseId = this.RelatedCaseId ?? 0,

            UserId      = this.UserId      ?? 0,
            RoleId      = this.RoleId      ?? 0,
            AttributeId = this.AttributeId ?? 0,

            Title       = TryParseToString(jsonNode["title"]),
            Description = TryParseToString(jsonNode["description"]),
            Status      = TryParseToString(jsonNode["status"]),

            Private = TryParseToBoolean(jsonNode["private"]),
        };
    }
}

public record EditParameters
{
    public class PatchField<T>
    {
        public bool Received { get; init; } = false;
        public T? Value { get; init; }
    }

    public bool HasChanges
    {
        get
        {
            return Title.Received || Description.Received || Status.Received || Private.Received;
        }
    }

    public required int RelatedCaseId { get; init; }

    public required int UserId { get; init; }
    public required int RoleId { get; init; }
    public required int AttributeId { get; init; }

    public PatchField<string?> Title { get; init; } = new();
    public PatchField<string?> Description { get; init; } = new();
    public PatchField<string?> Status { get; init; } = new();
    public PatchField<bool?> Private { get; init; } = new();
}

public record SearchInformationDto
{
    public IEnumerable<ItemProperties>? Items { get; init; }

    public class ItemProperties
    {
        public string? Title { get; init; }
        public string? Description { get; init; }

        public int? Id { get; init; }
        public int? UserId { get; init; }

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }
    }
}

public record SearchInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string Title { get; init; } = string.Empty;
        public required string Description { get; init; } = string.Empty;

        public required int Id { get; init; } = 0;
        public required int UserId { get; init; } = 0;

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }
    }

    public SearchInformationDto ToOrdinary()
    {
        return new SearchInformationDto
        {
            Items = this.Items.Select(x =>
                new SearchInformationDto.ItemProperties
                {
                    Title       = x.Title,
                    Description = x.Description,

                    Id = x.Id,
                    UserId = x.UserId,

                    CustomerId = x.CustomerId,
                    LawyerId   = x.LawyerId
                })
        };
    }
}


public record CountInformation
{
    public required long Count { get; init; }

    public CountInformationDto ToDto()
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

public record DetailsInformationDto
{
    public ItemProperties? Item { get; init; }

    public class ItemProperties
    {
        public string? Title { get; init; }
        public string? Description { get; init; }

        public int? Id { get; init; }
        public int? UserId { get; init; }

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }
    }
}

public record DetailsInformation
{
    public required ItemProperties Item { get; init; }

    public class ItemProperties
    {
        public required string Title { get; init; } = string.Empty;
        public required string Description { get; init; } = string.Empty;

        public required int Id { get; init; } = 0;
        public required int UserId { get; init; } = 0;

        public int? CustomerId { get; init; }
        public int? LawyerId { get; init; }
    }

    public DetailsInformationDto ToDto()
    {
        return new DetailsInformationDto
        {
            Item = new()
            {
                Title       = this.Item.Title,
                Description = this.Item.Description,

                Id     = this.Item.Id,
                UserId = this.Item.UserId,

                CustomerId = this.Item.CustomerId,
                LawyerId   = this.Item.LawyerId
            }
        };
    }
}