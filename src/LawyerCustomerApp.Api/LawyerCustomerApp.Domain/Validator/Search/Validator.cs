using LawyerCustomerApp.Domain.Search.Models.Common;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Search.Validator;

public class SearchCasesParametersDtoValidator : AbstractValidator<SearchCasesParametersDto>
{
    public SearchCasesParametersDtoValidator()
    {
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

        RuleFor(a => a.Query)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Query" });

        RuleFor(a => a.UserId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "UserId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" });

        RuleFor(a => a.Pagination)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Pagination" });

        RuleFor(a => a.Pagination)
            .SetValidator(new PaginationPropertiesValidator()!)
        .When(a => a.Pagination != null);
    }

    protected class PaginationPropertiesValidator : AbstractValidator<SearchCasesParametersDto.PaginationProperties>
    {
        public PaginationPropertiesValidator()
        {
            RuleFor(a => a.BeginIndex)
                .NotEmpty()
                .WithMessage("NotEmpty")
                .WithState(new string[] { "Pagination.BeginIndex" })
                .NotNull()
                .WithMessage("NotNull")
                .WithState(new string[] { "Pagination.BeginIndex" })
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState(new string[] { "Pagination.BeginIndex", "0" });

            RuleFor(a => a.EndIndex)
                .NotEmpty()
                .WithMessage("NotEmpty")
                .WithState(new string[] { "Pagination.EndIndex" })
                .NotNull()
                .WithMessage("NotNull")
                .WithState(new string[] { "Pagination.EndIndex" })
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState(new string[] { "Pagination.EndIndex", "0" });
        }
    }
}

public class SearchLawyersParametersDtoValidator : AbstractValidator<SearchLawyersParametersDto>
{
    public SearchLawyersParametersDtoValidator()
    {
        RuleFor(a => a.Query)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Query" });

        RuleFor(a => a.UserId)
            .NotEmpty()
            .WithMessage("NotEmpty")
            .WithState(new string[] { "UserId" })
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "UserId" });

        RuleFor(a => a.Pagination)
            .NotNull()
            .WithMessage("NotNull")
            .WithState(new string[] { "Pagination" });

        RuleFor(a => a.Pagination)
            .SetValidator(new PaginationPropertiesValidator()!)
        .When(a => a.Pagination != null);
    }

    protected class PaginationPropertiesValidator : AbstractValidator<SearchLawyersParametersDto.PaginationProperties>
    {
        public PaginationPropertiesValidator()
        {
            RuleFor(a => a.BeginIndex)
                .NotEmpty()
                .WithMessage("NotEmpty")
                .WithState(new string[] { "Pagination.BeginIndex" })
                .NotNull()
                .WithMessage("NotNull")
                .WithState(new string[] { "Pagination.BeginIndex" })
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState(new string[] { "Pagination.BeginIndex", "0" });

            RuleFor(a => a.EndIndex)
                .NotEmpty()
                .WithMessage("NotEmpty")
                .WithState(new string[] { "Pagination.EndIndex" })
                .NotNull()
                .WithMessage("NotNull")
                .WithState(new string[] { "Pagination.EndIndex" })
                .GreaterThanOrEqualTo(0)
                .WithMessage("GreaterThanOrEqualTo")
                .WithState(new string[] { "Pagination.EndIndex", "0" });
        }
    }
}