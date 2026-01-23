using Dappi.HeadlessCms.Models;
using FluentValidation;

namespace Dappi.HeadlessCms.Validators;

public class FieldRequestValidator : AbstractValidator<FieldRequest>
{
    public FieldRequestValidator()
    {
        RuleFor(x => x.Min)
            .Must((request, min) => FieldValidationHelper.ValidateMinValue(request.FieldType, min))
            .WithMessage("Min value is invalid for the field type.")
            .When(x => x.Min.HasValue);

        RuleFor(x => x.Max)
            .Must((request, max) => FieldValidationHelper.ValidateMaxValue(request.FieldType, max))
            .WithMessage("Max value is invalid for the field type.")
            .When(x => x.Max.HasValue);

        RuleFor(x => x)
            .Must(request => FieldValidationHelper.ValidateMinMaxRelationship(request.Min, request.Max))
            .WithMessage("Min value cannot be greater than max value.")
            .When(x => x.Min.HasValue && x.Max.HasValue);
    }
}

public class UpdateFieldRequestValidator : AbstractValidator<UpdateFieldRequest>
{
    public UpdateFieldRequestValidator()
    {
        RuleFor(x => x.Min)
            .Must((request, min) => FieldValidationHelper.ValidateMinValue(request.FieldType, min))
            .WithMessage("Min value is invalid for the field type.")
            .When(x => x.Min.HasValue);

        RuleFor(x => x.Max)
            .Must((request, max) => FieldValidationHelper.ValidateMaxValue(request.FieldType, max))
            .WithMessage("Max value is invalid for the field type.")
            .When(x => x.Max.HasValue);

        RuleFor(x => x)
            .Must(request => FieldValidationHelper.ValidateMinMaxRelationship(request.Min, request.Max))
            .WithMessage("Min value cannot be greater than max value.")
            .When(x => x.Min.HasValue && x.Max.HasValue);
    }
}

