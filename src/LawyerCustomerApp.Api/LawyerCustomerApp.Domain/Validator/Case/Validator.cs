using LawyerCustomerApp.Domain.Case.Models.Common;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Case.Validator;

public class CreateParametersDtoValidator : AbstractValidator<CreateParametersDto>
{
    public CreateParametersDtoValidator()
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

        RuleFor(a => a.Status)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Status" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Status" })
            .MaxLenght(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Status", "20" });

        RuleFor(a => a.EndDate)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "EndDate" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "EndDate" });

        RuleFor(a => a.BeginDate)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "BeginDate" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "BeginDate" });

        RuleFor(a => a.CustomerId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CustomerId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CustomerId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "CustomerId", "0" });

        RuleFor(a => a.LawyerId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "LawyerId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "LawyerId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "LawyerId", "0" });
    }
}

public class DeleteParametersDtoValidator : AbstractValidator<DeleteParametersDto>
{
    public DeleteParametersDtoValidator()
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

        RuleFor(a => a.Id)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "CustomerId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CustomerId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxLength")
            .WithState(new string[] { "CustomerId", "0" });
    }
}