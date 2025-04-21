using LawyerCustomerApp.Domain.Auth.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Auth.Validator;

public class AuthenticateParametersDtoValidator : AbstractValidator<AuthenticateParametersDto>
{
    public AuthenticateParametersDtoValidator()
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
    }
}

public class RefreshParametersDtoValidator : AbstractValidator<RefreshParametersDto>
{
    public RefreshParametersDtoValidator()
    {
        RuleFor(a => a.Token)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Token" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Token" })
            .MaxLenght(2500)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Token", "2500" });

        RuleFor(a => a.RefreshToken)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "RefreshToken" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RefreshToken" })
            .MaxLenght(300)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RefreshToken", "300" });
    }
}

public class InvalidateParametersDtoValidator : AbstractValidator<InvalidateParametersDto>
{
    public InvalidateParametersDtoValidator()
    {
        RuleFor(a => a.Token)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Token" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Token" })
            .MaxLenght(2500)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Token", "2500" });

        RuleFor(a => a.RefreshToken)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "RefreshToken" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RefreshToken" })
            .MaxLenght(300)
            .WithMessage("MaxLength")
            .WithState(new string[] { "RefreshToken", "300" });
    }
}

public class ValidateParametersDtoValidator : AbstractValidator<ValidateParametersDto>
{
    public ValidateParametersDtoValidator()
    {
        RuleFor(a => a.Token)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "Token" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Token" })
            .MaxLenght(2500)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Token", "2500" });
    }
}