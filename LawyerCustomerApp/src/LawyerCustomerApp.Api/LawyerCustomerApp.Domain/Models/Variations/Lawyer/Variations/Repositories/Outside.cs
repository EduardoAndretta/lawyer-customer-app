namespace LawyerCustomerApp.Domain.Lawyer.Repositories.Models;

public abstract record PermissionResult
{
    public record Search : PermissionResult
    {
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
    }

    public record Count : PermissionResult
    {
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
    }

    public record Details : PermissionResult
    {
        public bool HasViewUserPermission { get; init; } = false;
        public bool HasViewLawyerAccountUserPermission { get; init; } = false;

        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
    }

    public record Register : PermissionResult
    {
        public bool HasRegisterLawyerAccountUserPermission { get; init; } = false;
    }
}