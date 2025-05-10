using LawyerCustomerApp.Domain.Permission.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Permission.Validator;

#region Case

public class EnlistPermissionsFromCaseParametersDtoValidator : AbstractValidator<EnlistPermissionsFromCaseParametersDto>
{
    public EnlistPermissionsFromCaseParametersDtoValidator()
    {
        RuleFor(a => a.RelatedCaseId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedCaseId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RelatedCaseId", "0" });

        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}

public class GlobalPermissionsRelatedWithCaseParametersDtoValidator : AbstractValidator<GlobalPermissionsRelatedWithCaseParametersDto>
{
    public GlobalPermissionsRelatedWithCaseParametersDtoValidator()
    {
        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}

public class PermissionsRelatedWithCaseParametersDtoValidator : AbstractValidator<PermissionsRelatedWithCaseParametersDto>
{
    public PermissionsRelatedWithCaseParametersDtoValidator()
    {
        RuleFor(a => a.RelatedCaseId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedCaseId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RelatedCaseId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}

public class GrantPermissionsToCaseParametersDtoValidator : AbstractValidator<GrantPermissionsToCaseParametersDto>
{
    public GrantPermissionsToCaseParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.RelatedCaseId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "RelatedCaseId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedCaseId" });

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

    protected class PermissionPropertiesValidator : AbstractValidator<GrantPermissionsToCaseParametersDto.PermissionProperties>
    {
        public PermissionPropertiesValidator(string identifier)
        {
            RuleFor(a => a.UserId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId", "0" }));

            RuleFor(a => a.AttributeId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId", "0" }));

            RuleFor(a => a.RoleId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId", "0" }));

            RuleFor(a => a.PermissionId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId", "0" }));

        }
    }
}

public class RevokePermissionsToCaseParametersDtoValidator : AbstractValidator<RevokePermissionsToCaseParametersDto>
{
    public RevokePermissionsToCaseParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.AttributeId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "AttributeId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "AttributeId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.RelatedCaseId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "RelatedCaseId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedCaseId" });

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

    protected class PermissionPropertiesValidator : AbstractValidator<RevokePermissionsToCaseParametersDto.PermissionProperties>
    {
        public PermissionPropertiesValidator(string identifier)
        {
            RuleFor(a => a.UserId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId", "0" }));

            RuleFor(a => a.AttributeId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "AttributeId", "0" }));

            RuleFor(a => a.RoleId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId", "0" }));

            RuleFor(a => a.PermissionId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId", "0" }));

        }
    }
}

#endregion

#region User

public class EnlistPermissionsFromUserParametersDtoValidator : AbstractValidator<EnlistPermissionsFromUserParametersDto>
{
    public EnlistPermissionsFromUserParametersDtoValidator()
    {
        RuleFor(a => a.RelatedUserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedUserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RelatedUserId", "0" });

        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}



public class GlobalPermissionsRelatedWithUserParametersDtoValidator : AbstractValidator<GlobalPermissionsRelatedWithUserParametersDto>
{
    public GlobalPermissionsRelatedWithUserParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}

public class PermissionsRelatedWithUserParametersDtoValidator : AbstractValidator<PermissionsRelatedWithUserParametersDto>
{
    public PermissionsRelatedWithUserParametersDtoValidator()
    {
        RuleFor(a => a.RelatedUserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedUserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RelatedUserId", "0" });

        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}

public class GrantPermissionsToUserParametersDtoValidator : AbstractValidator<GrantPermissionsToUserParametersDto>
{
    public GrantPermissionsToUserParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.RelatedUserId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "RelatedUserId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedUserId" });

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

    protected class PermissionPropertiesValidator : AbstractValidator<GrantPermissionsToUserParametersDto.PermissionProperties>
    {
        public PermissionPropertiesValidator(string identifier)
        {
            RuleFor(a => a.UserId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId", "0" }));

            RuleFor(a => a.RoleId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId", "0" }));

            RuleFor(a => a.PermissionId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId", "0" }));

        }
    }
}

public class RevokePermissionsToUserParametersDtoValidator : AbstractValidator<RevokePermissionsToUserParametersDto>
{
    public RevokePermissionsToUserParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RoleId", "0" });

        RuleFor(a => a.RelatedUserId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "RelatedUserId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedUserId" });

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

    protected class PermissionPropertiesValidator : AbstractValidator<RevokePermissionsToUserParametersDto.PermissionProperties>
    {
        public PermissionPropertiesValidator(string identifier)
        {
            RuleFor(a => a.UserId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "UserId", "0" }));

            RuleFor(a => a.RoleId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "RoleId", "0" }));

            RuleFor(a => a.PermissionId)
                .NotNull()
                .WithMessage("NotNull")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId" }))
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState((Identifier: identifier, Parameters: new string[] { "PermissionId", "0" }));

        }
    }
}

#endregion

public class SearchEnabledUsersToGrantPermissionsParametersDtoValidator : AbstractValidator<SearchEnabledUsersToGrantPermissionsParametersDto>
{
    public SearchEnabledUsersToGrantPermissionsParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}


public class SearchEnabledUsersToRevokePermissionsParametersDtoValidator : AbstractValidator<SearchEnabledUsersToRevokePermissionsParametersDto>
{
    public SearchEnabledUsersToRevokePermissionsParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}