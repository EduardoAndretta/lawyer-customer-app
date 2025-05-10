using LawyerCustomerApp.External.Models;

namespace LawyerCustomerApp.Domain.User.Repositories.Models;

public abstract record PermissionResult
{
    public record Search : PermissionResult
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
    }

    public record Count : PermissionResult
    {
        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewOwnUserPermission { get; init; } = false;
    }

    public record Details : PermissionResult
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
    }


    public record Register : PermissionResult
    {
        public bool HasRegisterUserPermission { get; init; } = false;
    }

    public record Edit : PermissionResult
    {
        public bool HasEditUserPermission { get; init; } = false;
        public bool HasEditLawyerAccountUserPermission { get; init; } = false;
        public bool HasEditCustomerAccountUserPermission { get; init; } = false;

        public bool HasEditOwnUserPermission { get; init; } = false;

        public bool HasEditAnyUserPermission { get; init; } = false;
        public bool HasEditAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasEditAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewOwnUserPermission { get; init; } = false;
        public bool HasViewOwnLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewOwnCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewPublicUserPermission { get; init; } = false;
        public bool HasViewPublicLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewPublicCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewAnyUserPermission { get; init; } = false;
        public bool HasViewAnyLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewAnyCustomerAccountUserPermission { get; init; } = false;

        public bool HasViewUserPermission { get; init; } = false;
        public bool HasViewLawyerAccountUserPermission { get; init; } = false;
        public bool HasViewCustomerAccountUserPermission { get; init; } = false;
    }
}