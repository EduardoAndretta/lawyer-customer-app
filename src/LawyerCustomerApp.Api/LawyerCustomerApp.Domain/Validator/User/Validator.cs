using LawyerCustomerApp.Domain.User.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.User.Validator;

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
            .MaxLenght(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Email", "50" });

        RuleFor(a => a.Password)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Password" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Password" })
            .MaxLenght(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Password", "50" });

        RuleFor(a => a.Name)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Name" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Name" })
            .MaxLenght(50)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Name", "50" });
    }
}
