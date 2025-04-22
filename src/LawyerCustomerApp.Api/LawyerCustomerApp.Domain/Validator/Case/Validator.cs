using LawyerCustomerApp.Domain.Case.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Case.Validator;

public class SearchParametersDtoValidator : AbstractValidator<SearchParametersDto>
{
    public SearchParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "UserId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.Persona)
           .NotEmpty()
           .WithMessage("NotEmpty")
           .WithState(new string[] { "Persona" })
           .NotNull()
           .WithMessage("NotNull")
           .WithState(new string[] { "Persona" });

        RuleFor(a => a.Query)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Title" })
            .MaxLenght(50)
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
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "UserId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.Persona)
           .NotEmpty()
           .WithMessage("NotEmpty")
           .WithState(new string[] { "Persona" })
           .NotNull()
           .WithMessage("NotNull")
           .WithState(new string[] { "Persona" });

        RuleFor(a => a.Title)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Title" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Title" })
            .MaxLenght(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Title", "50" });

        RuleFor(a => a.Description)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Description" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Description" })
            .MaxLenght(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Description", "100" });
    }
}

public class AssignLawyerParametersDtoValidator : AbstractValidator<AssignLawyerParametersDto>
{
    public AssignLawyerParametersDtoValidator()
    {
        RuleFor(a => a.UserId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "UserId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "UserId", "0" });

        RuleFor(a => a.CaseId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CaseId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CaseId" });

        RuleFor(a => a.Persona)
           .NotEmpty()
           .WithMessage("NotEmpty")
           .WithState(new string[] { "Persona" })
           .NotNull()
           .WithMessage("NotNull")
           .WithState(new string[] { "Persona" });

        RuleFor(a => a.LawyerId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "LawyerId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "LawyerId" });
    }
}