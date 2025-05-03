using LawyerCustomerApp.External.Models;
using LawyerCustomerApp.External.Responses.Success.Models;
using Microsoft.Extensions.Localization;
using System.Collections.ObjectModel;

namespace LawyerCustomerApp.Domain.User.Responses.Repositories.Success;

internal class Success { }

internal class GrantPermissionSuccess : ConstructorWithDetails<GrantPermissionSuccess.DetailsVariation>
{
    public override string Identity => "GrantPermissionSuccess";
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

internal class RevokePermissionSuccess : ConstructorWithDetails<RevokePermissionSuccess.DetailsVariation>
{
    public override string Identity => "RevokePermissionSuccess";
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