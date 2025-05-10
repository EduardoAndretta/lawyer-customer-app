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
}