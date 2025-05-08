namespace LawyerCustomerApp.Domain.Customer.Repositories.Models;

public abstract record PermissionResult
{
    public record Search : PermissionResult
    {
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnCustomerAccountUserPermission { get; init; } = false;
    }

    public record Details : PermissionResult
    {
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnCustomerAccountUserPermission { get; init; } = false;
    }

    public record Register : PermissionResult
    {
        public bool HasRegisterCustomerAccountUserPermission { get; init; } = false;
    }
}