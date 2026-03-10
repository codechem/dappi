using Dappi.HeadlessCms.Extensions;
using Dappi.HeadlessCms.Models;
using FluentValidation;

namespace Dappi.HeadlessCms.Validators;

public partial class ModelRequestValidator : AbstractValidator<ModelRequest>
{
    public ModelRequestValidator()
    {
        RuleFor(x => x.ModelName)
            .NotEmpty()
            .WithMessage("Model name must be provided.")
            .MaximumLength(50)
            .WithMessage("Model name must not exceed 50 characters.")
            .Must(modelName => modelName.IsValidClassNameOrPropertyName())
            .WithMessage("Model name is invalid.");
    }
}
