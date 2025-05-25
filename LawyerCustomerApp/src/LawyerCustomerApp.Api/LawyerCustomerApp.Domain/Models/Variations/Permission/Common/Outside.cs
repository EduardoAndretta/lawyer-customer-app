using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LawyerCustomerApp.Domain.Permission.Common.Models;

#region Case

public record EnlistPermissionsFromCaseParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

    public int? RelatedCaseId { get; init; }

    public EnlistPermissionsFromCaseParameters ToOrdinary()
    {
        return new EnlistPermissionsFromCaseParameters
        {
            UserId        = this.UserId        ?? 0,
            RoleId        = this.RoleId        ?? 0,
            AttributeId   = this.AttributeId   ?? 0,

            RelatedCaseId = this.RelatedCaseId ?? 0
        };
    }
}

public class EnlistPermissionsFromCaseParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }
    public required int AttributeId { get; init; }

    public required int RelatedCaseId { get; init; }
}

public record GlobalPermissionsRelatedWithCaseParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

    public GlobalPermissionsRelatedWithCaseParameters ToOrdinary()
    {
        return new()
        {
            UserId      = this.UserId      ?? 0,
            RoleId      = this.RoleId      ?? 0,
            AttributeId = this.AttributeId ?? 0
        };
    }
}

public class GlobalPermissionsRelatedWithCaseParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }
    public required int AttributeId { get; init; }
}

public record PermissionsRelatedWithCaseParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

    public int? RelatedCaseId { get; init; }

    public PermissionsRelatedWithCaseParameters ToOrdinary()
    {
        return new()
        {
            UserId      = this.UserId      ?? 0,
            RoleId      = this.RoleId      ?? 0,
            AttributeId = this.AttributeId ?? 0,

            RelatedCaseId = this.RelatedCaseId ?? 0
        };
    }
}

public class PermissionsRelatedWithCaseParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }
    public required int AttributeId { get; init; }

    public required int RelatedCaseId { get; init; }

}

public record GrantPermissionsToCaseParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

    public int? RelatedCaseId { get; init; }

    public required IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? AttributeId { get; init; }
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
    }

    public GrantPermissionsToCaseParameters ToOrdinary()
    {
        return new GrantPermissionsToCaseParameters
        {
            UserId      = this.UserId      ?? 0,
            RoleId      = this.RoleId      ?? 0,
            AttributeId = this.AttributeId ?? 0,

            RelatedCaseId = this.RelatedCaseId ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new GrantPermissionsToCaseParameters.PermissionProperties
                {
                    AttributeId  = item?.AttributeId  ?? 0,
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                }) 
            ?? new Collection<GrantPermissionsToCaseParameters.PermissionProperties>()
        };
    }
}

public class GrantPermissionsToCaseParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }
    public required int AttributeId { get; init; }

    public required int RelatedCaseId { get; init; }

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


public record RevokePermissionsToCaseParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? AttributeId { get; init; }

    public int? RelatedCaseId { get; init; }

    public required IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? AttributeId { get; init; }
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
    }

    public RevokePermissionsToCaseParameters ToOrdinary()
    {
        return new RevokePermissionsToCaseParameters
        {
            UserId      = this.UserId      ?? 0,
            RoleId      = this.RoleId      ?? 0,
            AttributeId = this.AttributeId ?? 0,

            RelatedCaseId = this.RelatedCaseId ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new RevokePermissionsToCaseParameters.PermissionProperties
                {
                    AttributeId  = item?.AttributeId  ?? 0,
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                }) 
            ?? new Collection<RevokePermissionsToCaseParameters.PermissionProperties>()
        };
    }
}

public class RevokePermissionsToCaseParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }
    public required int AttributeId { get; init; }

    public required int RelatedCaseId { get; init; }

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


public record EnlistedPermissionsFromCaseInformationDto
{
    public IEnumerable<ItemProperties> Items { get; init; } = new Collection<ItemProperties>();

    public class ItemProperties
    {
        public string? UserName { get; init; }
        public string? PermissionName { get; init; }
        public string? RoleName { get; init; }
        public string? AttributeName { get; init; }

        public int? UserId { get; init; }
        public int? PermissionId { get; init; }
        public int? AttributeId { get; init; }
        public int? RoleId { get; init; }
    }
}

public record EnlistedPermissionsFromCaseInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string UserName { get; init; }
        public required string PermissionName { get; init; }
        public required string RoleName { get; init; }
        public required string AttributeName { get; init; }

        public required int UserId { get; init; }
        public required int PermissionId { get; init; }
        public required int AttributeId { get; init; }
        public required int RoleId { get; init; }
    }

    public EnlistedPermissionsFromCaseInformationDto ToDto()
    {
        return new EnlistedPermissionsFromCaseInformationDto
        {
            Items = this.Items.Select(x =>
                new EnlistedPermissionsFromCaseInformationDto.ItemProperties
                {
                    UserName       = x.UserName,
                    PermissionName = x.PermissionName,
                    AttributeName  = x.AttributeName,
                    RoleName       = x.RoleName,

                    UserId       = x.UserId,
                    PermissionId = x.PermissionId,
                    AttributeId  = x.AttributeId,
                    RoleId       = x.RoleId
                })
        };
    }
}

public record GlobalPermissionsRelatedWithCaseInformationDto
{
    public bool? RegisterCase { get; init; } = false;

    public bool? EditOwnCase { get; init; } = false;
    public bool? EditAnyCase { get; init; } = false;

    public bool? ViewAnyCase { get; init; } = false;
    public bool? ViewOwnCase { get; init; } = false;
    public bool? ViewPublicCase { get; init; } = false;

    public bool? ViewPermissionsOwnCase { get; init; } = false;
    public bool? ViewPermissionsAnyCase { get; init; } = false;

    public bool? AssignLawyerOwnCase { get; init; } = false;
    public bool? AssignLawyerAnyCase { get; init; } = false;

    public bool? AssignCustomerOwnCase { get; init; } = false;
    public bool? AssignCustomerAnyCase { get; init; } = false;

    public bool? GrantPermissionsOwnCase { get; init; } = false;
    public bool? GrantPermissionsAnyCase { get; init; } = false;

    public bool? RevokePermissionsOwnCase { get; init; } = false;
    public bool? RevokePermissionsAnyCase { get; init; } = false;
}

public record GlobalPermissionsRelatedWithCaseInformation
{
    public required bool RegisterCase { get; init; }

    public required bool EditOwnCase { get; init; }
    public required bool EditAnyCase { get; init; }

    public required bool ViewAnyCase { get; init; }
    public required bool ViewOwnCase { get; init; }
    public required bool ViewPublicCase { get; init; }

    public required bool ViewPermissionsOwnCase { get; init; }
    public required bool ViewPermissionsAnyCase { get; init; }

    public required bool AssignLawyerOwnCase { get; init; }
    public required bool AssignLawyerAnyCase { get; init; }

    public required bool AssignCustomerOwnCase { get; init; }
    public required bool AssignCustomerAnyCase { get; init; }

    public required bool GrantPermissionsOwnCase { get; init; }
    public required bool GrantPermissionsAnyCase { get; init; }

    public required bool RevokePermissionsOwnCase { get; init; }
    public required bool RevokePermissionsAnyCase { get; init; }

    public GlobalPermissionsRelatedWithCaseInformationDto ToDto()
    { 
        return new()
        {
            RegisterCase = this.RegisterCase,

            EditOwnCase = this.EditOwnCase,
            EditAnyCase = this.EditAnyCase,

            ViewAnyCase    = this.ViewAnyCase,
            ViewOwnCase    = this.ViewOwnCase,
            ViewPublicCase = this.ViewPublicCase,

            ViewPermissionsOwnCase = this.ViewPermissionsOwnCase,
            ViewPermissionsAnyCase = this.ViewPermissionsAnyCase,

            AssignLawyerOwnCase = this.AssignLawyerOwnCase,
            AssignLawyerAnyCase = this.AssignLawyerAnyCase,

            AssignCustomerOwnCase = this.AssignCustomerOwnCase,
            AssignCustomerAnyCase = this.AssignCustomerAnyCase,

            GrantPermissionsOwnCase = this.GrantPermissionsOwnCase,
            GrantPermissionsAnyCase = this.GrantPermissionsAnyCase,

            RevokePermissionsOwnCase = this.RevokePermissionsOwnCase,
            RevokePermissionsAnyCase = this.RevokePermissionsAnyCase
        };
    }
}

public record PermissionsRelatedWithCaseInformationDto
{
    public bool? EditCase { get; init; } = false;

    public bool? ViewCase { get; init; } = false;

    public bool? ViewPermissionsCase { get; init; } = false;

    public bool? AssignLawyerCase { get; init; } = false;

    public bool? AssignCustomerCase { get; init; } = false;

    public bool? GrantPermissionsCase { get; init; } = false;

    public bool? RevokePermissionsCase { get; init; } = false;
}

public record PermissionsRelatedWithCaseInformation
{
    public required bool EditCase { get; init; }

    public required bool ViewCase { get; init; }

    public required bool ViewPermissionsCase { get; init; }

    public required bool AssignLawyerCase { get; init; }

    public required bool AssignCustomerCase { get; init; }

    public required bool GrantPermissionsCase { get; init; }

    public PermissionsRelatedWithCaseInformationDto ToDto()
    { 
        return new()
        {
            EditCase = this.EditCase,

            ViewCase = this.ViewCase,

            ViewPermissionsCase = this.ViewPermissionsCase,

            AssignLawyerCase = this.AssignLawyerCase,

            AssignCustomerCase = this.AssignCustomerCase,

            GrantPermissionsCase = this.GrantPermissionsCase
        };
    }
}

#endregion

#region User

public record EnlistPermissionsFromUserParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? RelatedUserId { get; init; }

    public EnlistPermissionsFromUserParameters ToOrdinary()
    {
        return new EnlistPermissionsFromUserParameters
        {
            UserId        = this.UserId        ?? 0,
            RoleId        = this.RoleId        ?? 0,

            RelatedUserId = this.RelatedUserId ?? 0,
        };
    }
}

public class EnlistPermissionsFromUserParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }

    public required int RelatedUserId { get; init; }
}


public record GlobalPermissionsRelatedWithUserParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public GlobalPermissionsRelatedWithUserParameters ToOrdinary()
    {
        return new GlobalPermissionsRelatedWithUserParameters
        {
            UserId = this.UserId ?? 0,
            RoleId = this.RoleId ?? 0
        };
    }
}

public class GlobalPermissionsRelatedWithUserParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }
}

public record PermissionsRelatedWithUserParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? RelatedUserId { get; init; }

    public PermissionsRelatedWithUserParameters ToOrdinary()
    {
        return new PermissionsRelatedWithUserParameters
        {
            UserId = this.UserId ?? 0,
            RoleId = this.RoleId ?? 0,

            RelatedUserId = this.RelatedUserId ?? 0
        };
    }
}

public class PermissionsRelatedWithUserParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }

    public required int RelatedUserId { get; init; }
}

public record GrantPermissionsToUserParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public int? RelatedUserId { get; init; }

    public required IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
        public int? AttributeId { get; init; }
    }

    public GrantPermissionsToUserParameters ToOrdinary()
    {
        return new GrantPermissionsToUserParameters
        {
            UserId      = this.UserId      ?? 0,
            RoleId      = this.RoleId      ?? 0,

            RelatedUserId = this.RelatedUserId ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new GrantPermissionsToUserParameters.PermissionProperties
                {
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                    AttributeId  = item?.AttributeId
                }) 
            ?? new Collection<GrantPermissionsToUserParameters.PermissionProperties>()
        };
    }
}

public class GrantPermissionsToUserParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }

    public required int RelatedUserId { get; init; }

    public required IEnumerable<PermissionProperties> Permissions { get; init; }

    public class PermissionProperties
    {
        public readonly Guid Id = Guid.NewGuid();

        public required int PermissionId { get; init; }
        public required int UserId { get; init; }
        public required int RoleId { get; init; }
        public int? AttributeId { get; init; }
    }
}


public record RevokePermissionsToUserParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }


    public int? RelatedUserId { get; init; }

    public required IEnumerable<PermissionProperties?>? Permissions { get; init; }

    public class PermissionProperties
    {
        public int? PermissionId { get; init; }
        public int? UserId { get; init; }
        public int? RoleId { get; init; }
        public int? AttributeId { get; init; }

    }

    public RevokePermissionsToUserParameters ToOrdinary()
    {
        return new RevokePermissionsToUserParameters
        {
            UserId      = this.UserId ?? 0,
            RoleId      = this.RoleId ?? 0,

            RelatedUserId = this.RelatedUserId ?? 0,

            Permissions = this.Permissions?.Select(item =>
                new RevokePermissionsToUserParameters.PermissionProperties
                {
                    PermissionId = item?.PermissionId ?? 0,
                    UserId       = item?.UserId       ?? 0,
                    RoleId       = item?.RoleId       ?? 0,
                    AttributeId  = item?.AttributeId,
                }) 
            ?? new Collection<RevokePermissionsToUserParameters.PermissionProperties>()
        };
    }
}

public class RevokePermissionsToUserParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }

    public required int RelatedUserId { get; init; }

    public required IEnumerable<PermissionProperties> Permissions { get; init; }

    public class PermissionProperties
    {
        public readonly Guid Id = Guid.NewGuid();

        public required int PermissionId { get; init; }
        public required int UserId { get; init; }
        public required int RoleId { get; init; }
        public int? AttributeId { get; init; }
    }
}

public record EnlistedPermissionsFromUserInformationDto
{
    public IEnumerable<ItemProperties> Items { get; init; } = new Collection<ItemProperties>();

    public class ItemProperties
    {
        public string? UserName { get; init; }
        public string? PermissionName { get; init; }
        public string? RoleName { get; init; }
        public string? AttributeName { get; init; }

        public int? UserId { get; init; }
        public int? PermissionId { get; init; }
        public int? RoleId { get; init; }
        public int? AttributeId { get; init; }
    }
}

public record EnlistedPermissionsFromUserInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string UserName { get; init; }
        public required string PermissionName { get; init; }
        public required string RoleName { get; init; }
        public string? AttributeName { get; init; }

        public required int UserId { get; init; }
        public required int PermissionId { get; init; }
        public required int RoleId { get; init; }
        public int? AttributeId { get; init; }
    }

    public EnlistedPermissionsFromUserInformationDto ToDto()
    {
        return new EnlistedPermissionsFromUserInformationDto
        {
            Items = this.Items.Select(x =>
                new EnlistedPermissionsFromUserInformationDto.ItemProperties
                {
                    UserName       = x.UserName,
                    PermissionName = x.PermissionName,
                    RoleName       = x.RoleName,
                    AttributeName  = x.AttributeName,

                    UserId       = x.UserId,
                    PermissionId = x.PermissionId,
                    RoleId       = x.RoleId,
                    AttributeId  = x.AttributeId
                })
        };
    }
}

public record GlobalPermissionsRelatedWithUserInformationDto
{
    public bool? GrantPermissionsOwnUser { get; init; } = false;
    public bool? GrantPermissionsAnyUser { get; init; } = false;

    public bool? GrantPermissionsOwnLawyerAccountUser { get; init; } = false;
    public bool? GrantPermissionsAnyLawyerAccountUser { get; init; } = false;

    public bool? GrantPermissionsOwnCustomerAccountUser { get; init; } = false;
    public bool? GrantPermissionsAnyCustomerAccountUser { get; init; } = false;


    public bool? RevokePermissionsOwnUser { get; init; } = false;
    public bool? RevokePermissionsAnyUser { get; init; } = false;

    public bool? RevokePermissionsOwnLawyerAccountUser { get; init; } = false;
    public bool? RevokePermissionsAnyLawyerAccountUser { get; init; } = false;

    public bool? RevokePermissionsOwnCustomerAccountUser { get; init; } = false;
    public bool? RevokePermissionsAnyCustomerAccountUser { get; init; } = false;


    public bool? RegisterUser { get; init; } = false;
    public bool? RegisterLawyerAccountUser { get; init; } = false;
    public bool? RegisterCustomerAccountUser { get; init; } = false;


    public bool? EditOwnUser { get; init; } = false;
    public bool? EditAnyUser { get; init; } = false;

    public bool? EditOwnLawyerAccountUser { get; init; } = false;
    public bool? EditAnyLawyerAccountUser { get; init; } = false;

    public bool? EditOwnCustomerAccountUser { get; init; } = false;
    public bool? EditAnyCustomerAccountUser { get; init; } = false;


    public bool? ViewOwnUser { get; init; } = false;
    public bool? ViewAnyUser { get; init; } = false;
    public bool? ViewPublicUser { get; init; } = false;

    public bool? ViewOwnLawyerAccountUser { get; init; } = false;
    public bool? ViewAnyLawyerAccountUser { get; init; } = false;
    public bool? ViewPublicLawyerAccountUser { get; init; } = false;

    public bool? ViewOwnCustomerAccountUser { get; init; } = false;
    public bool? ViewAnyCustomerAccountUser { get; init; } = false;
    public bool? ViewPublicCustomerAccountUser { get; init; } = false;


    public bool? ViewPermissionsOwnUser { get; init; } = false;
    public bool? ViewPermissionsAnyUser { get; init; } = false;

    public bool? ViewPermissionsOwnLawyerAccountUser { get; init; } = false;
    public bool? ViewPermissionsAnyLawyerAccountUser { get; init; } = false;

    public bool? ViewPermissionsOwnCustomerAccountUser { get; init; } = false;
    public bool? ViewPermissionsAnyCustomerAccountUser { get; init; } = false;
}

public record GlobalPermissionsRelatedWithUserInformation
{
    public bool GrantPermissionsOwnUser { get; init; } = false;
    public bool GrantPermissionsAnyUser { get; init; } = false;

    public bool GrantPermissionsOwnLawyerAccountUser { get; init; } = false;
    public bool GrantPermissionsAnyLawyerAccountUser { get; init; } = false;

    public bool GrantPermissionsOwnCustomerAccountUser { get; init; } = false;
    public bool GrantPermissionsAnyCustomerAccountUser { get; init; } = false;


    public bool RevokePermissionsOwnUser { get; init; } = false;
    public bool RevokePermissionsAnyUser { get; init; } = false;

    public bool RevokePermissionsOwnLawyerAccountUser { get; init; } = false;
    public bool RevokePermissionsAnyLawyerAccountUser { get; init; } = false;

    public bool RevokePermissionsOwnCustomerAccountUser { get; init; } = false;
    public bool RevokePermissionsAnyCustomerAccountUser { get; init; } = false;

    public bool RegisterUser { get; init; } = false;
    public bool RegisterLawyerAccountUser { get; init; } = false;
    public bool RegisterCustomerAccountUser { get; init; } = false;

    
    public bool EditOwnUser { get; init; } = false;
    public bool EditAnyUser { get; init; } = false;

    public bool EditOwnLawyerAccountUser { get; init; } = false;
    public bool EditAnyLawyerAccountUser { get; init; } = false;

    public bool EditOwnCustomerAccountUser { get; init; } = false;
    public bool EditAnyCustomerAccountUser { get; init; } = false;


    public bool ViewOwnUser { get; init; } = false;
    public bool ViewAnyUser { get; init; } = false;
    public bool ViewPublicUser { get; init; } = false;

    public bool ViewOwnLawyerAccountUser { get; init; } = false;
    public bool ViewAnyLawyerAccountUser { get; init; } = false;
    public bool ViewPublicLawyerAccountUser { get; init; } = false;

    public bool ViewOwnCustomerAccountUser { get; init; } = false;
    public bool ViewAnyCustomerAccountUser { get; init; } = false;
    public bool ViewPublicCustomerAccountUser { get; init; } = false;


    public bool ViewPermissionsOwnUser { get; init; } = false;
    public bool ViewPermissionsAnyUser { get; init; } = false;

    public bool ViewPermissionsOwnLawyerAccountUser { get; init; } = false;
    public bool ViewPermissionsAnyLawyerAccountUser { get; init; } = false;

    public bool ViewPermissionsOwnCustomerAccountUser { get; init; } = false;
    public bool ViewPermissionsAnyCustomerAccountUser { get; init; } = false;

    public GlobalPermissionsRelatedWithUserInformationDto ToDto()
    {
        return new()
        {
            GrantPermissionsOwnUser = this.GrantPermissionsOwnUser,
            GrantPermissionsAnyUser = this.GrantPermissionsAnyUser,

            GrantPermissionsOwnLawyerAccountUser = this.GrantPermissionsOwnLawyerAccountUser,
            GrantPermissionsAnyLawyerAccountUser = this.GrantPermissionsAnyLawyerAccountUser,

            GrantPermissionsOwnCustomerAccountUser = this.GrantPermissionsOwnCustomerAccountUser,
            GrantPermissionsAnyCustomerAccountUser = this.GrantPermissionsAnyCustomerAccountUser,

            RevokePermissionsOwnUser = this.RevokePermissionsOwnUser,
            RevokePermissionsAnyUser = this.RevokePermissionsAnyUser,

            RevokePermissionsOwnLawyerAccountUser = this.RevokePermissionsOwnLawyerAccountUser,
            RevokePermissionsAnyLawyerAccountUser = this.RevokePermissionsAnyLawyerAccountUser,

            RevokePermissionsOwnCustomerAccountUser = this.RevokePermissionsOwnCustomerAccountUser,
            RevokePermissionsAnyCustomerAccountUser = this.RevokePermissionsAnyCustomerAccountUser,

            RegisterUser                = this.RegisterUser,
            RegisterLawyerAccountUser   = this.RegisterLawyerAccountUser,
            RegisterCustomerAccountUser = this.RegisterCustomerAccountUser,

            EditOwnUser = this.EditOwnUser,
            EditAnyUser = this.EditAnyUser,

            EditOwnLawyerAccountUser  = this.EditOwnLawyerAccountUser,
            EditAnyLawyerAccountUser  = this.EditAnyLawyerAccountUser,

            EditOwnCustomerAccountUser = this.EditOwnCustomerAccountUser,
            EditAnyCustomerAccountUser = this.EditAnyCustomerAccountUser,

            ViewOwnUser    = this.ViewOwnUser,
            ViewAnyUser    = this.ViewAnyUser,
            ViewPublicUser = this.ViewPublicUser,

            ViewOwnLawyerAccountUser    = this.ViewOwnLawyerAccountUser,
            ViewAnyLawyerAccountUser    = this.ViewAnyLawyerAccountUser,
            ViewPublicLawyerAccountUser = this.ViewPublicLawyerAccountUser,

            ViewOwnCustomerAccountUser    = this.ViewOwnCustomerAccountUser,
            ViewAnyCustomerAccountUser    = this.ViewAnyCustomerAccountUser,
            ViewPublicCustomerAccountUser = this.ViewPublicCustomerAccountUser,

            ViewPermissionsOwnUser = this.ViewPermissionsOwnUser,
            ViewPermissionsAnyUser = this.ViewPermissionsAnyUser,

            ViewPermissionsOwnLawyerAccountUser = this.ViewPermissionsOwnLawyerAccountUser,
            ViewPermissionsAnyLawyerAccountUser = this.ViewPermissionsAnyLawyerAccountUser,

            ViewPermissionsOwnCustomerAccountUser = this.ViewPermissionsOwnCustomerAccountUser,
            ViewPermissionsAnyCustomerAccountUser = this.ViewPermissionsAnyCustomerAccountUser
        };
    }
}

public record PermissionsRelatedWithUserInformationDto
{
    public bool? GrantPermissionsUser { get; init; } = false;
    public bool? GrantPermissionsLawyerAccountUser { get; init; } = false;
    public bool? GrantPermissionsCustomerAccountUser { get; init; } = false;

    public bool? RevokePermissionsUser { get; init; } = false;
    public bool? RevokePermissionsLawyerAccountUser { get; init; } = false;
    public bool? RevokePermissionsCustomerAccountUser { get; init; } = false;

    public bool? EditUser { get; init; } = false;
    public bool? EditLawyerAccountUser { get; init; } = false;
    public bool? EditCustomerAccountUser { get; init; } = false;

    public bool? ViewUser { get; init; } = false;
    public bool? ViewLawyerAccountUser { get; init; } = false;
    public bool? ViewCustomerAccountUser { get; init; } = false;

    public bool? ViewPermissionsUser { get; init; } = false;
    public bool? ViewPermissionsLawyerAccountUser { get; init; } = false;
    public bool? ViewPermissionsCustomerAccountUser { get; init; } = false;
}

public record PermissionsRelatedWithUserInformation
{
    public required bool GrantPermissionsUser { get; init; }
    public required bool GrantPermissionsLawyerAccountUser { get; init; }
    public required bool GrantPermissionsCustomerAccountUser { get; init; }

    public required bool RevokePermissionsUser { get; init; }
    public required bool RevokePermissionsLawyerAccountUser { get; init; }
    public required bool RevokePermissionsCustomerAccountUser { get; init; }

    public required bool EditUser { get; init; }
    public required bool EditLawyerAccountUser { get; init; }
    public required bool EditCustomerAccountUser { get; init; }

    public required bool ViewUser { get; init; }
    public required bool ViewLawyerAccountUser { get; init; }
    public required bool ViewCustomerAccountUser { get; init; }

    public required bool ViewPermissionsUser { get; init; }
    public required bool ViewPermissionsLawyerAccountUser { get; init; }
    public required bool ViewPermissionsCustomerAccountUser { get; init; }

    public PermissionsRelatedWithUserInformationDto ToDto()
    {
        return new()
        {
            GrantPermissionsUser                = this.GrantPermissionsUser,
            GrantPermissionsLawyerAccountUser   = this.GrantPermissionsLawyerAccountUser,
            GrantPermissionsCustomerAccountUser = this.GrantPermissionsCustomerAccountUser,

            RevokePermissionsUser                = this.RevokePermissionsUser,
            RevokePermissionsLawyerAccountUser   = this.RevokePermissionsLawyerAccountUser,
            RevokePermissionsCustomerAccountUser = this.RevokePermissionsCustomerAccountUser,

            EditUser                = this.EditUser,
            EditLawyerAccountUser   = this.EditLawyerAccountUser,
            EditCustomerAccountUser = this.EditCustomerAccountUser,

            ViewUser                = this.ViewUser,
            ViewLawyerAccountUser   = this.ViewLawyerAccountUser,
            ViewCustomerAccountUser = this.ViewCustomerAccountUser,

            ViewPermissionsUser                = this.ViewPermissionsUser,
            ViewPermissionsLawyerAccountUser   = this.ViewPermissionsLawyerAccountUser,
            ViewPermissionsCustomerAccountUser = this.ViewPermissionsCustomerAccountUser
        };
    }
}

#endregion

public record SearchEnabledUsersToGrantPermissionsParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public string? Query { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public SearchEnabledUsersToGrantPermissionsParameters ToOrdinary()
    {
        return new SearchEnabledUsersToGrantPermissionsParameters
        {
            Query = this.Query ?? string.Empty,

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

public class SearchEnabledUsersToGrantPermissionsParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }

    public required string Query { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int Begin { get; init; }
        public required int End { get; init; }
    }
}


public record SearchEnabledUsersToRevokePermissionsParametersDto
{
    [JsonIgnore]
    public int? UserId { get; init; }

    [JsonIgnore]
    public int? RoleId { get; init; }

    public string? Query { get; init; }

    public PaginationProperties? Pagination { get; init; }

    public SearchEnabledUsersToRevokePermissionsParameters ToOrdinary()
    {
        return new SearchEnabledUsersToRevokePermissionsParameters
        {
            Query = this.Query ?? string.Empty,

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

public class SearchEnabledUsersToRevokePermissionsParameters
{
    public required int UserId { get; init; }
    public required int RoleId { get; init; }

    public required string Query { get; init; }

    public required PaginationProperties Pagination { get; init; }

    public class PaginationProperties
    {
        public required int Begin { get; init; }
        public required int End { get; init; }
    }
}


public record SearchEnabledUsersToGrantPermissionsInformationDto
{
    public IEnumerable<ItemProperties>? Items { get; init; }

    public class ItemProperties
    {
        public string? Name { get; init; }

        public int? UserId { get; init; }

        public bool? HasLawyerAccount { get; init; }
        public bool? HasCustomerAccount { get; init; }

        public bool? CanBeGrantAsUser { get; init; }
        public bool? CanBeGrantAsLawyer { get; init; }
        public bool? CanBeGrantAsCustomer { get; init; }
    }
}

public record SearchEnabledUsersToGrantPermissionsInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string Name { get; init; }

        public required int UserId { get; init; }

        public required bool HasCustomerAccount { get; init; }
        public required bool HasLawyerAccount { get; init; }

        public required bool CanBeGrantAsUser { get; init; }
        public required bool CanBeGrantAsLawyer { get; init; }
        public required bool CanBeGrantAsCustomer { get; init; }
    }

    public SearchEnabledUsersToGrantPermissionsInformationDto ToDto()
    {
        return new SearchEnabledUsersToGrantPermissionsInformationDto
        {
            Items = this.Items.Select(x =>
                new SearchEnabledUsersToGrantPermissionsInformationDto.ItemProperties
                {
                    Name = x.Name,

                    UserId = x.UserId,

                    HasCustomerAccount = x.HasCustomerAccount,
                    HasLawyerAccount   = x.HasLawyerAccount,

                    CanBeGrantAsUser     = x.CanBeGrantAsUser,
                    CanBeGrantAsLawyer   = x.CanBeGrantAsLawyer,
                    CanBeGrantAsCustomer = x.CanBeGrantAsCustomer
                })
        };
    }
}

public record SearchEnabledUsersToRevokePermissionsInformationDto
{
    public IEnumerable<ItemProperties>? Items { get; init; }

    public class ItemProperties
    {
        public string? Name { get; init; }

        public int? UserId { get; init; }

        public bool? HasLawyerAccount { get; init; }
        public bool? HasCustomerAccount { get; init; }

        public bool? CanBeRevokeAsUser { get; init; }
        public bool? CanBeRevokeAsLawyer { get; init; }
        public bool? CanBeRevokeAsCustomer { get; init; }
    }
}

public record SearchEnabledUsersToRevokePermissionsInformation
{
    public required IEnumerable<ItemProperties> Items { get; init; }

    public class ItemProperties
    {
        public required string Name { get; init; }

        public required int UserId { get; init; }

        public required bool HasCustomerAccount { get; init; }
        public required bool HasLawyerAccount { get; init; }

        public required bool CanBeRevokeAsUser { get; init; }
        public required bool CanBeRevokeAsLawyer { get; init; }
        public required bool CanBeRevokeAsCustomer { get; init; }
    }

    public SearchEnabledUsersToRevokePermissionsInformationDto ToDto()
    {
        return new SearchEnabledUsersToRevokePermissionsInformationDto
        {
            Items = this.Items.Select(x =>
                new SearchEnabledUsersToRevokePermissionsInformationDto.ItemProperties
                {
                    Name = x.Name,

                    UserId = x.UserId,

                    HasCustomerAccount = x.HasCustomerAccount,
                    HasLawyerAccount   = x.HasLawyerAccount,

                    CanBeRevokeAsUser     = x.CanBeRevokeAsUser,
                    CanBeRevokeAsLawyer   = x.CanBeRevokeAsLawyer,
                    CanBeRevokeAsCustomer = x.CanBeRevokeAsCustomer
                })
        };
    }
}
