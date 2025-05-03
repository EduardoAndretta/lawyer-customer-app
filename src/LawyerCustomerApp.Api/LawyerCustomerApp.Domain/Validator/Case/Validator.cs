using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Case.Validator;

public class SearchParametersDtoValidator : AbstractValidator<SearchParametersDto>
{
    public SearchParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.Query)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Title" })
            .MaxLength(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Title", "100" });

        RuleFor(a => a.Pagination)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Pagination" });

        RuleFor(a => a.Pagination!.End)
           .NotNull()
           .WithMessage("NotNull")
           .WithState(new string[] { "Pagination.End" })
                .When(a => a.Pagination != null);

        RuleFor(a => a.Pagination!.Begin)
           .NotNull()
           .WithMessage("NotNull")
           .WithState(new string[] { "Pagination.Begin" })
                .When(a => a.Pagination != null);
    }
}

public class RegisterParametersDtoValidator : AbstractValidator<RegisterParametersDto>
{
    public RegisterParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.Title)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Title" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Title" })
            .MaxLength(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Title", "50" });

        RuleFor(a => a.Description)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Description" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Description" })
            .MaxLength(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Description", "100" });
    }
}

public class AssignLawyerParametersDtoValidator : AbstractValidator<AssignLawyerParametersDto>
{
    public AssignLawyerParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.CaseId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CaseId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CaseId" });

        RuleFor(a => a.LawyerId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "LawyerId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "LawyerId" });
    }
}

public class AssignCustomerParametersDtoValidator : AbstractValidator<AssignCustomerParametersDto>
{
    public AssignCustomerParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.CaseId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CaseId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CaseId" });

        RuleFor(a => a.CustomerId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CustomerId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CustomerId" });
    }
}

public class GrantPermissionsParametersDtoValidator : AbstractValidator<GrantPermissionsParametersDto>
{
    public GrantPermissionsParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.CaseId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CaseId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CaseId" });

        RuleFor(x => x.Permissions)
            .Custom(async (x, context, cancellationToken) =>
            {
                if (x == null)
                    return;

                foreach (var (item, index) in x.Select((item, index) => (item, index)))
                {
                    var identifier = index.ToString();

                    if (item == null)
                    {
                        context.AddFailure(new ValidationFailure(identifier, "NotNull", (Identifier: identifier, Parameters: new string[] { "Item" })));

                        continue;
                    }

                    var validationResult = await new PermissionPropertiesValidator(identifier).ValidateAsync(item, cancellationToken);

                    foreach (var error in validationResult.Errors)
                    {
                        context.AddFailure(error);
                    }
                }
            });
    }

    protected class PermissionPropertiesValidator : AbstractValidator<GrantPermissionsParametersDto.PermissionProperties>
    {
        public PermissionPropertiesValidator(string identifier)
        {
            RuleFor(a => a.UserId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId", "0" }));

            RuleFor(a => a.AttributeId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId", "0" }));

            RuleFor(a => a.RoleId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId", "0" }));

            RuleFor(a => a.PermissionId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId", "0" }));

        }
    }
}

public class RevokePermissionsParametersDtoValidator : AbstractValidator<RevokePermissionsParametersDto>
{
    public RevokePermissionsParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.CaseId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CaseId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CaseId" });

        RuleFor(x => x.Permissions)
            .Custom(async (x, context, cancellationToken) =>
            {
                if (x == null)
                    return;

                foreach (var (item, index) in x.Select((item, index) => (item, index)))
                {
                    var identifier = index.ToString();

                    if (item == null)
                    {
                        context.AddFailure(new ValidationFailure(identifier, "NotNull", (Identifier: identifier, Parameters: new string[] { "Item" })));

                        continue;
                    }

                    var validationResult = await new PermissionPropertiesValidator(identifier).ValidateAsync(item, cancellationToken);

                    foreach (var error in validationResult.Errors)
                    {
                        context.AddFailure(error);
                    }
                }
            });
    }

    protected class PermissionPropertiesValidator : AbstractValidator<RevokePermissionsParametersDto.PermissionProperties>
    {
        public PermissionPropertiesValidator(string identifier)
        {
            RuleFor(a => a.UserId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId", "0" }));

            RuleFor(a => a.AttributeId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId", "0" }));

            RuleFor(a => a.RoleId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId", "0" }));

            RuleFor(a => a.PermissionId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId" }))
                .GreaterThan(0)
                .WithMessage("GreaterThan")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId", "0" }));

        }
    }
}