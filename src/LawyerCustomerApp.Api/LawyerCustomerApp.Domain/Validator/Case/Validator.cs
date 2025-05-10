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

        RuleFor(a => a.CaseId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CaseId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "CaseId", "0" });

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

public class EditParametersDtoValidator : AbstractValidator<EditParametersDto>
{
    public EditParametersDtoValidator()
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