using System.Collections.ObjectModel;

namespace LawyerCustomerApp.Domain.Case.Common.Models;

public class SearchParametersDto
{
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

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

public class RegisterParametersDto
{
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public string? Title { get; init; }
    public string? Description { get; init; }

    public int? CustomerId { get; init; }
    public int? LawyerId { get; init; }

    public RegisterParameters ToOrdinary()
    {
        return new RegisterParameters
        {
            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,


            Title       = this.Title       ?? string.Empty,
            Description = this.Description ?? string.Empty,

            CustomerId = this.CustomerId ?? 0,
            LawyerId   = this.LawyerId   ?? 0,

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

    public int? CustomerId { get; init; }
    public int? LawyerId { get; init; }
}

public class AssignLawyerParametersDto
{
    public int? CaseId { get; init; }
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public int? LawyerId { get; init; }
   
    public AssignLawyerParameters ToOrdinary()
    {
        return new AssignLawyerParameters
        {
            CaseId      = this.CaseId ?? 0,
            UserId      = this.UserId ?? 0,
            AttributeId = this.UserId ?? 0,
            RoleId      = this.UserId ?? 0,

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

public class AssignCustomerParametersDto
{
    public int? CaseId { get; init; }
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public int? CustomerId { get; init; }

    public AssignCustomerParameters ToOrdinary()
    {
        return new AssignCustomerParameters
        {
            CaseId      = this.CaseId ?? 0,
            UserId      = this.UserId ?? 0,
            AttributeId = this.UserId ?? 0,
            RoleId      = this.UserId ?? 0,

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

public class GrantPermissionsParametersDto
{
    public int? CaseId { get; init; }
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public required IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? AttributeId { get; init; }
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
    }

    public GrantPermissionsParameters ToOrdinary()
    {
        return new GrantPermissionsParameters
        {
            CaseId      = this.CaseId      ?? 0,
            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new GrantPermissionsParameters.PermissionProperties
                {
                    AttributeId  = item?.AttributeId  ?? 0,
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                }) 
            ?? new Collection<GrantPermissionsParameters.PermissionProperties>()
        };
    }
}

public class GrantPermissionsParameters
{
    public required int CaseId { get; init; }
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required IEnumerable<PermissionProperties> Permissions { get; init; }

    public class PermissionProperties
    {
        public readonly Guid Id = Guid.NewGuid();

        public required int AttributeId { get; init; }
        public required int PermissionId { get; init; }
        public required int UserId { get; init; }
        public required int RoleId { get; init; }
    }
}

public class RevokePermissionsParametersDto
{
    public int? CaseId { get; init; }
    public int? UserId { get; init; }
    public int? AttributeId { get; init; }
    public int? RoleId { get; init; }

    public required IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? AttributeId { get; init; }
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
    }

    public RevokePermissionsParameters ToOrdinary()
    {
        return new RevokePermissionsParameters
        {
            CaseId      = this.CaseId      ?? 0,
            UserId      = this.UserId      ?? 0,
            AttributeId = this.AttributeId ?? 0,
            RoleId      = this.RoleId      ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new RevokePermissionsParameters.PermissionProperties
                {
                    AttributeId  = item?.AttributeId  ?? 0,
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                }) 
            ?? new Collection<RevokePermissionsParameters.PermissionProperties>()
        };
    }
}

public class RevokePermissionsParameters
{
    public required int CaseId { get; init; }
    public required int UserId { get; init; }
    public required int AttributeId { get; init; }
    public required int RoleId { get; init; }

    public required IEnumerable<PermissionProperties> Permissions { get; init; }

    public class PermissionProperties
    {
        public readonly Guid Id = Guid.NewGuid();

        public required int AttributeId { get; init; }
        public required int PermissionId { get; init; }
        public required int UserId { get; init; }
        public required int RoleId { get; init; }
    }
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