using LawyerCustomerApp.Domain.Lawyer.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Lawyer.Validator;

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

        RuleFor(a => a.Address)
            .MaxLength(100)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Address", "100" })
                .When(a => !string.IsNullOrWhiteSpace(a.Phone));

        RuleFor(a => a.Phone)
            .MaxLength(15)
            .WithMessage("MaxLength")
            .WithState(new string[] { "Phone", "15" })
                .When(a => !string.IsNullOrWhiteSpace(a.Phone));
    }
}
