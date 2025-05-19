using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Responses.Success.Models;
using Microsoft.Extensions.Localization;
using System.Collections.ObjectModel;

namespace LawyerCustomerApp.Domain.Permission.Responses.Repositories.Success;

internal class Success { }

#region Case

internal class GrantPermissionsToCaseSuccess : ConstructorWithDetails<GrantPermissionsToCaseSuccess.DetailsVariation>
{
    public override string Identity => "GrantPermissionsToCaseSuccess";
    public override Type Resource => typeof(Success);

    public override object DetailsMap(IServiceProvider serviceProvider, IStringLocalizer resource, DetailsVariation details)
    {
        Collection<object> response = new();

        foreach (var item in details.Result)
        {
            response.Add(new
            {
                UserId       = item.UserId,
                PermissionId = item.PermissionId,
                RoleId       = item.RoleId,
                AttributeId  = item.AttributeId,

                Result = item.Result.BuildResponse(serviceProvider)
            });
        }

        return new
        {
            IncludedItems = details.IncludedItems,

            Result = response
        };
    }

    public record DetailsVariation : Details
    {
        public class Fields
        {
            public required int UserId { get; set; }
            public required int PermissionId { get; set; }
            public required int RoleId { get; set; }
            public required int AttributeId { get; set; }

            public required Result Result { get; set; }
        }

        public int IncludedItems { get; init; } = 0;
        public IEnumerable<Fields> Result { get; init; } = new Collection<Fields>();
    }
}

internal class RevokePermissionsToCaseSuccess : ConstructorWithDetails<RevokePermissionsToCaseSuccess.DetailsVariation>
{
    public override string Identity => "RevokePermissionsToCaseSuccess";
    public override Type Resource => typeof(Success);

    public override object DetailsMap(IServiceProvider serviceProvider, IStringLocalizer resource, DetailsVariation details)
    {
        Collection<object> response = new();

        foreach (var item in details.Result)
        {
            response.Add(new
            {
                UserId       = item.UserId,
                PermissionId = item.PermissionId,
                RoleId       = item.RoleId,
                AttributeId  = item.AttributeId,

                Result = item.Result.BuildResponse(serviceProvider)
            });
        }

        return new
        {
            DeletedItems = details.DeletedItems,

            Result = response
        };
    }

    public record DetailsVariation : Details
    {
        public class Fields
        {
            public required int UserId { get; set; }
            public required int PermissionId { get; set; }
            public required int RoleId { get; set; }
            public required int AttributeId { get; set; }

            public required Result Result { get; set; }
        }

        public int DeletedItems { get; init; } = 0;
        public IEnumerable<Fields> Result { get; init; } = new Collection<Fields>();
    }
}

#endregion

#region User

internal class GrantPermissionsToUserSuccess : ConstructorWithDetails<GrantPermissionsToUserSuccess.DetailsVariation>
{
    public override string Identity => "GrantPermissionsToUserSuccess";
    public override Type Resource => typeof(Success);

    public override object DetailsMap(IServiceProvider serviceProvider, IStringLocalizer resource, DetailsVariation details)
    {
        Collection<object> response = new();

        foreach (var item in details.Result)
        {
            response.Add(new
            {
                UserId       = item.UserId,
                PermissionId = item.PermissionId,
                RoleId       = item.RoleId,
                AttributeId  = item.AttributeId,

                Result = item.Result.BuildResponse(serviceProvider)
            });
        }

        return new
        {
            IncludedItems = details.IncludedItems,

            Result = response
        };
    }

    public record DetailsVariation : Details
    {
        public class Fields
        {
            public required int UserId { get; set; }
            public required int PermissionId { get; set; }
            public required int RoleId { get; set; }
            public int? AttributeId { get; set; }

            public required Result Result { get; set; }
        }

        public int IncludedItems { get; init; } = 0;
        public IEnumerable<Fields> Result { get; init; } = new Collection<Fields>();
    }
}

internal class RevokePermissionsToUserSuccess : ConstructorWithDetails<RevokePermissionsToUserSuccess.DetailsVariation>
{
    public override string Identity => "RevokePermissionsToUserSuccess";
    public override Type Resource => typeof(Success);

    public override object DetailsMap(IServiceProvider serviceProvider, IStringLocalizer resource, DetailsVariation details)
    {
        Collection<object> response = new();

        foreach (var item in details.Result)
        {
            response.Add(new
            {
                UserId       = item.UserId,
                PermissionId = item.PermissionId,
                RoleId       = item.RoleId,
                AttributeId  = item.AttributeId,

                Result = item.Result.BuildResponse(serviceProvider)
            });
        }

        return new
        {
            DeletedItems = details.DeletedItems,

            Result = response
        };
    }

    public record DetailsVariation : Details
    {
        public class Fields
        {
            public required int UserId { get; set; }
            public required int PermissionId { get; set; }
            public required int RoleId { get; set; }
            public int? AttributeId { get; set; }

            public required Result Result { get; set; }
        }

        public int DeletedItems { get; init; } = 0;
        public IEnumerable<Fields> Result { get; init; } = new Collection<Fields>();
    }
}

#endregion