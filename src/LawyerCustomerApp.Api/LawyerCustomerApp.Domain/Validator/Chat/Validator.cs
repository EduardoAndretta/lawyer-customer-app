using LawyerCustomerApp.Domain.Chat.Models.Common;
using LawyerCustomerApp.External.Validation;

namespace LawyerCustomerApp.Domain.Chat.Validator;

public class GetParametersDtoValidator : AbstractValidator<GetParametersDto>
{
    public GetParametersDtoValidator()
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

    protected class PaginationPropertiesValidator : AbstractValidator<GetParametersDto.PaginationProperties>
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