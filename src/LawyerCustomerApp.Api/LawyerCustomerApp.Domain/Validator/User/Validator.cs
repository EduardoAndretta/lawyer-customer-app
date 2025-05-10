using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.User.Validator;

public class SearchParametersDtoValidator : AbstractValidator<SearchParametersDto>
{
    public SearchParametersDtoValidator()
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

public class CountParametersDtoValidator : AbstractValidator<CountParametersDto>
{
    public CountParametersDtoValidator()
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

        RuleFor(a => a.Query)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Title" })
            .MaxLength(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Title", "100" });
    }
}

public class DetailsParametersDtoValidator : AbstractValidator<DetailsParametersDto>
{
    public DetailsParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.RelatedUserId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RelatedUserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RelatedUserId", "0" });

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });
    }
}

public class RegisterParametersDtoValidator : AbstractValidator<RegisterParametersDto>
{
    public RegisterParametersDtoValidator()
    {
        RuleFor(a => a.Email)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Email" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Email" })
            .MaxLength(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Email", "50" });

        RuleFor(a => a.Password)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Password" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Password" })
            .MaxLength(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Password", "50" });

        RuleFor(a => a.Name)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Name" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Name" })
            .MaxLength(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Name", "50" });
    }
}

public class EditParametersDtoValidator : AbstractValidator<EditParametersDto>
{
    public EditParametersDtoValidator()
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