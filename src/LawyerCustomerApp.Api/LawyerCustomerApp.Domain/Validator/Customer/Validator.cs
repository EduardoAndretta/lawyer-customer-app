using LawyerCustomerApp.Domain.Customer.Common.Models;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Customer.Validator;

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

        RuleFor(a => a.CustomerId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "CustomerId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "CustomerId", "0" });

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

        RuleFor(a => a.RoleId)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "RoleId" })
            .GreaterThanOrEqualTo(0)
            .WithMessage("GreaterThanOrEqualTo")
            .WithState(new string[] { "RoleId", "0" });

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
