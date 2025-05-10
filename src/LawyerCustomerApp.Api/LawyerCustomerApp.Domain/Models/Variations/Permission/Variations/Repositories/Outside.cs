using LawyerCustomerApp.External.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace LawyerCustomerApp.Domain.Permission.Repositories.Models;

public abstract class InternalValues
{
    #region Case

    public record GrantPermissionsToCase
    {
        public DataPropreties Data { get; set; } = new();

        public record DataPropreties
        {
            public void Finish(Guid id)
            {
                if (!Items.TryGetValue(id, out var item))
                    throw new Exception("Failed to get item from items.");

                var finishedIdentifier = new FinishedIdentfier()
                {
                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId,
                    AttributeId  = item.AttributeId,

                    Result = item.Result
                };

                if (!Finished.TryAdd(item.Id, finishedIdentifier))
                    throw new Exception("Failed to add the item to invalidated items.");

                if (!Items.Remove(item.Id))
                    throw new Exception("Failed to remove the invalidated items from items.");
            }

            public Dictionary<Guid, FinishedIdentfier> Finished { get; init; } = new Dictionary<Guid, FinishedIdentfier>();
            public IDictionary<Guid, Item> Items { get; init; } = new Dictionary<Guid, Item>();


            public record FinishedIdentfier
            {
                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }
                public required int AttributeId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }

            public record Item
            {
                // [From parameters]
                public Guid Id { get; set; } = Guid.NewGuid();

                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }
                public required int AttributeId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }
        }
    }

    public record RevokePermissionsToCase
    {
        public DataPropreties Data { get; set; } = new();

        public record DataPropreties
        {
            public void Finish(Guid id)
            {
                if (!Items.TryGetValue(id, out var item))
                    throw new Exception("Failed to get item from items.");

                var finishedIdentifier = new FinishedIdentfier()
                {
                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId,
                    AttributeId  = item.AttributeId,

                    Result = item.Result
                };

                if (!Finished.TryAdd(item.Id, finishedIdentifier))
                    throw new Exception("Failed to add the item to invalidated items.");

                if (!Items.Remove(item.Id))
                    throw new Exception("Failed to remove the invalidated items from items.");
            }

            public Dictionary<Guid, FinishedIdentfier> Finished { get; init; } = new Dictionary<Guid, FinishedIdentfier>();
            public IDictionary<Guid, Item> Items { get; init; } = new Dictionary<Guid, Item>();


            public record FinishedIdentfier
            {
                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }
                public required int AttributeId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }

            public record Item
            {
                // [From parameters]
                public Guid Id { get; set; } = Guid.NewGuid();

                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }
                public required int AttributeId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }
        }
    }

    #endregion

    #region User

    public record GrantPermissionsToUser
    {
        public DataPropreties Data { get; set; } = new();

        public record DataPropreties
        {
            public void Finish(Guid id)
            {
                if (!Items.TryGetValue(id, out var item))
                    throw new Exception("Failed to get item from items.");

                var finishedIdentifier = new FinishedIdentfier()
                {
                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId,

                    Result = item.Result
                };

                if (!Finished.TryAdd(item.Id, finishedIdentifier))
                    throw new Exception("Failed to add the item to invalidated items.");

                if (!Items.Remove(item.Id))
                    throw new Exception("Failed to remove the invalidated items from items.");
            }

            public Dictionary<Guid, FinishedIdentfier> Finished { get; init; } = new Dictionary<Guid, FinishedIdentfier>();
            public IDictionary<Guid, Item> Items { get; init; } = new Dictionary<Guid, Item>();


            public record FinishedIdentfier
            {
                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }

            public record Item
            {
                // [From parameters]
                public Guid Id { get; set; } = Guid.NewGuid();

                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }
        }
    }

    public record RevokePermissionsToUser
    {
        public DataPropreties Data { get; set; } = new();

        public record DataPropreties
        {
            public void Finish(Guid id)
            {
                if (!Items.TryGetValue(id, out var item))
                    throw new Exception("Failed to get item from items.");

                var finishedIdentifier = new FinishedIdentfier()
                {
                    UserId       = item.UserId,
                    PermissionId = item.PermissionId,
                    RoleId       = item.RoleId,

                    Result = item.Result
                };

                if (!Finished.TryAdd(item.Id, finishedIdentifier))
                    throw new Exception("Failed to add the item to invalidated items.");

                if (!Items.Remove(item.Id))
                    throw new Exception("Failed to remove the invalidated items from items.");
            }

            public Dictionary<Guid, FinishedIdentfier> Finished { get; init; } = new Dictionary<Guid, FinishedIdentfier>();
            public IDictionary<Guid, Item> Items { get; init; } = new Dictionary<Guid, Item>();


            public record FinishedIdentfier
            {
                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }

            public record Item
            {
                // [From parameters]
                public Guid Id { get; set; } = Guid.NewGuid();

                public required int UserId { get; set; }
                public required int PermissionId { get; set; }
                public required int RoleId { get; set; }

                // [Result]
                public Result Result { get; set; } = new Result();
            }
        }
    }

    #endregion
}

public abstract record PermissionResult
{
    #region Case

    public record EnlistPermissionsFromCase : PermissionResult
    {
        public bool HasViewPermissionsOwnCasePermission { get; init; } = false;
        public bool HasViewPermissionsAnyCasePermission { get; init; } = false;
        public bool HasViewPermissionsCasePermission { get; init; } = false;

        public bool HasViewOwnCasePermission { get; init; } = false;
        public bool HasViewAnyCasePermission { get; init; } = false;
        public bool HasViewPublicCasePermission { get; init; } = false;
        public bool HasViewCasePermission { get; init; } = false;
    }

    public record GrantPermissionsToCase : PermissionResult
    {
        public bool HasGrantPermissionsAnyCasePermission = false;
        public bool HasGrantPermissionsOwnCasePermission = false;
        public bool HasGrantPermissionsCasePermission = false;

        public bool HasViewAnyCasePermission = false;
        public bool HasViewOwnCasePermission = false;
        public bool HasViewPublicCasePermission = false;
        public bool HasViewCasePermission = false;

        public bool HasGrantPermissionsAnyUserPermission = false;
        public bool HasGrantPermissionsAnyLawyerAccountUserPermission = false;
        public bool HasGrantPermissionsAnyCustomerAccountUserPermission = false;

        public bool HasGrantPermissionsOwnUserPermission = false;
        public bool HasGrantPermissionsOwnLawyerAccountUserPermission = false;
        public bool HasGrantPermissionsOwnCustomerAccountUserPermission = false;

        public bool HasGrantPermissionsUserPermission = false;
        public bool HasGrantPermissionsLawyerAccountUserPermission = false;
        public bool HasGrantPermissionsCustomerAccountUserPermission = false;

        public bool HasViewOwnUserPermission = false;
        public bool HasViewOwnLawyerAccountUserPermission = false;
        public bool HasViewOwnCustomerAccountUserPermission = false;

        public bool HasViewPublicUserPermission = false;
        public bool HasViewPublicLawyerAccountUserPermission = false;
        public bool HasViewPublicCustomerAccountUserPermission = false;

        public bool HasViewAnyUserPermission = false;
        public bool HasViewAnyLawyerAccountUserPermission = false;
        public bool HasViewAnyCustomerAccountUserPermission = false;

        public bool HasViewUserPermission = false;
        public bool HasViewLawyerAccountUserPermission = false;
        public bool HasViewCustomerAccountUserPermission = false;
    }

    public record RevokePermissionsToCase : PermissionResult
    {
        public bool HasRevokePermissionsAnyCasePermission = false;
        public bool HasRevokePermissionsOwnCasePermission = false;
        public bool HasRevokePermissionsCasePermission = false;

        public bool HasViewAnyCasePermission = false;
        public bool HasViewOwnCasePermission = false;
        public bool HasViewPublicCasePermission = false;
        public bool HasViewCasePermission = false;

        public bool HasRevokePermissionsAnyUserPermission = false;
        public bool HasRevokePermissionsAnyLawyerAccountUserPermission = false;
        public bool HasRevokePermissionsAnyCustomerAccountUserPermission = false;

        public bool HasRevokePermissionsOwnUserPermission = false;
        public bool HasRevokePermissionsOwnLawyerAccountUserPermission = false;
        public bool HasRevokePermissionsOwnCustomerAccountUserPermission = false;

        public bool HasRevokePermissionsUserPermission = false;
        public bool HasRevokePermissionsLawyerAccountUserPermission = false;
        public bool HasRevokePermissionsCustomerAccountUserPermission = false;

        public bool HasViewOwnUserPermission = false;
        public bool HasViewOwnLawyerAccountUserPermission = false;
        public bool HasViewOwnCustomerAccountUserPermission = false;

        public bool HasViewPublicUserPermission = false;
        public bool HasViewPublicLawyerAccountUserPermission = false;
        public bool HasViewPublicCustomerAccountUserPermission = false;

        public bool HasViewAnyUserPermission = false;
        public bool HasViewAnyLawyerAccountUserPermission = false;
        public bool HasViewAnyCustomerAccountUserPermission = false;

        public bool HasViewUserPermission = false;
        public bool HasViewLawyerAccountUserPermission = false;
        public bool HasViewCustomerAccountUserPermission = false;
    }


    #endregion

    #region User

    public record EnlistPermissionsFromUser : PermissionResult
    {
        public bool HasViewPermissionsAnyUserPermission { get; init; } = false;
        public bool HasViewPermissionsAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewPermissionsAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPermissionsOwnUserPermission { get; init; } = false;
        public bool HasViewPermissionsOwnLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewPermissionsOwnCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPermissionsUserPermission { get; init; } = false;
        public bool HasViewPermissionsLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewPermissionsCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewOwnCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewUserPermission { get; init; } = false;
        public bool HasViewLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewCustomerAccountUserPermission { get; init; } = false;
    }
    public record GrantPermissionsToUser : PermissionResult
    {
        public bool HasGrantPermissionsOwnUserPermission { get; init; } = false;
        public bool HasGrantPermissionsUserPermission { get; init; } = false;
        public bool HasGrantPermissionsAnyUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewUserPermission { get; init; } = false;
    }

    public record RevokePermissionsToUser : PermissionResult
    {
        public bool HasRevokePermissionsOwnUserPermission { get; init; } = false;
        public bool HasRevokePermissionsUserPermission { get; init; } = false;
        public bool HasRevokePermissionsAnyUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewUserPermission { get; init; } = false;
    }

    #endregion

    public record SearchEnabledUsersToGrantPermissions : PermissionResult
    {
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewOwnCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewUserPermission { get; init; } = false;
        public bool HasViewLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewCustomerAccountUserPermission { get; init; } = false;

        public bool HasGrantPermissionsOwnUserPermission { get; init; } = false;
        public bool HasGrantPermissionsOwnLawyerAccountUserPermission { get; init; } = false;
        public bool HasGrantPermissionsOwnCustomerAccountUserPermission { get; init; } = false;

        public bool HasGrantPermissionsAnyUserPermission { get; init; } = false;
        public bool HasGrantPermissionsAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasGrantPermissionsAnyCustomerAccountUserPermission { get; init; } = false;
    }

    public record SearchEnabledUsersToRevokePermissions : PermissionResult
    {
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewOwnCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewUserPermission { get; init; } = false;
        public bool HasViewLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewCustomerAccountUserPermission { get; init; } = false;

        public bool HasRevokePermissionsOwnUserPermission { get; init; } = false;
        public bool HasRevokePermissionsOwnLawyerAccountUserPermission { get; init; } = false;
        public bool HasRevokePermissionsOwnCustomerAccountUserPermission { get; init; } = false;

        public bool HasRevokePermissionsAnyUserPermission { get; init; } = false;
        public bool HasRevokePermissionsAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasRevokePermissionsAnyCustomerAccountUserPermission { get; init; } = false;
    }
}