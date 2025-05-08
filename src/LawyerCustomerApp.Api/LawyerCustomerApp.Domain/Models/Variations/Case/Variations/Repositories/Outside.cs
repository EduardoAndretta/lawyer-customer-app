using LawyerCustomerApp.External.Models;

namespace LawyerCustomerApp.Domain.Case.Repositories.Models;

internal record AssignLawyerDatabaseInformation
{
    public bool Private { get; set; } = false;
}

internal record AssignCustomerDatabaseInformation
{
    public bool Private { get; set; } = false;
}

public abstract class InternalValues
{
    public record GrantPermission
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
                public required int PermissionId{ get; set; }
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

    public record RevokePermission
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
}

public abstract record PermissionResult
{
    public record Search : PermissionResult
    {
        public bool HasViewOwnCasePermission { get; init; } = false;
        public bool HasViewAnyCasePermission { get; init; } = false;
        public bool HasViewPublicCasePermission { get; init; } = false;
    }

    public record Register : PermissionResult
    {
        public bool HasRegisterCasePermission { get; init; } = false;
    }

    public record AssignLawyer : PermissionResult
    {
        public bool HasAssignLawyerOwnCasePermission { get; init; } = false;
        public bool HasAssignLawyerCasePermission { get; init; } = false;
        public bool HasAssignLawyerAnyCasePermission { get; init; } = false;

        public bool HasViewOwnCasePermission { get; init; } = false;
        public bool HasViewCasePermission { get; init; } = false;
        public bool HasViewAnyCasePermission { get; init; } = false;
        public bool HasViewPublicCasePermission { get; init; } = false;
    }

    public record AssignCustomer : PermissionResult
    {
        public bool HasAssignCustomerOwnCasePermission { get; init; } = false;
        public bool HasAssignCustomerCasePermission { get; init; } = false;
        public bool HasAssignCustomerAnyCasePermission { get; init; } = false;

        public bool HasViewOwnCasePermission { get; init; } = false;
        public bool HasViewCasePermission { get; init; } = false;
        public bool HasViewAnyCasePermission { get; init; } = false;
        public bool HasViewPublicCasePermission { get; init; } = false;
    }


    public record Edit : PermissionResult
    {
        public bool HasEditOwnCasePermission { get; init; } = false;
        public bool HasEditCasePermission { get; init; } = false;
        public bool HasEditAnyCasePermission { get; init; } = false;

        public bool HasViewOwnCasePermission { get; init; } = false;
        public bool HasViewCasePermission { get; init; } = false;
        public bool HasViewAnyCasePermission { get; init; } = false;
        public bool HasViewPublicCasePermission { get; init; } = false;
    }

    public record GrantPermissions : PermissionResult
    {
        public bool HasGrantPermissionsOwnCasePermission { get; init; } = false;
        public bool HasGrantPermissionsCasePermission { get; init; } = false;
        public bool HasGrantPermissionsAnyCasePermission { get; init; } = false;

        public bool HasViewOwnCasePermission { get; init; } = false;
        public bool HasViewCasePermission { get; init; } = false;
        public bool HasViewAnyCasePermission { get; init; } = false;
        public bool HasViewPublicCasePermission { get; init; } = false;

        public record SpecificUser
        {
            public bool HasGrantPermissionsAnyUserPermission { get; init; } = false;
            public bool HasGrantPermissionsAnyLawyerAccountUserPermission { get; init; } = false;
            public bool HasGrantPermissionsAnyCustomerAccountUserPermission { get; init; } = false;

            public bool HasViewAnyUserPermission { get; init; } = false;
            public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;
            public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

            public bool HasViewPublicUserPermission { get; init; } = false;
            public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;
            public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;
        }  
    }

    public record RevokePermissions : PermissionResult
    {
        public bool HasRevokePermissionsOwnCasePermission { get; init; } = false;
        public bool HasRevokePermissionsCasePermission { get; init; } = false;
        public bool HasRevokePermissionsAnyCasePermission { get; init; } = false;

        public bool HasViewOwnCasePermission { get; init; } = false;
        public bool HasViewCasePermission { get; init; } = false;
        public bool HasViewAnyCasePermission { get; init; } = false;
        public bool HasViewPublicCasePermission { get; init; } = false;

        public record SpecificUser
        {
            public bool HasRevokePermissionsAnyUserPermission { get; init; } = false;
            public bool HasRevokePermissionsAnyLawyerAccountUserPermission { get; init; } = false;
            public bool HasRevokePermissionsAnyCustomerAccountUserPermission { get; init; } = false;

            public bool HasRevokePermissionsOwnUserPermission { get; init; } = false;
            public bool HasRevokePermissionsOwnLawyerAccountUserPermission { get; init; } = false;
            public bool HasRevokePermissionsOwnCustomerAccountUserPermission { get; init; } = false;

            public bool HasViewAnyUserPermission { get; init; } = false;
            public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;
            public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

            public bool HasViewPublicUserPermission { get; init; } = false;
            public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;
            public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;

            public bool HasViewOwnUserPermission { get; init; } = false;
            public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
            public bool HasViewOwnCustomerAccountUserPermission { get; init; } = false;
        }
    }
}