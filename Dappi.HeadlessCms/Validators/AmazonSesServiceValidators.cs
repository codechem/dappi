using Dappi.HeadlessCms.Models;
using FluentValidation;

namespace Dappi.HeadlessCms.Validators;

public class AwsAccountValidator : AbstractValidator<AwsAccountOptions>
{
    public AwsAccountValidator()
    {
        RuleFor(x => x.AccessKey).NotEmpty();
        RuleFor(x => x.SecretKey).NotEmpty();
        RuleFor(x => x.Region).NotEmpty();
        RuleFor(x => x.BucketName).NotEmpty();
    }
}

public class AwsSesValidator : AbstractValidator<AwsSesOptions>
{
    public AwsSesValidator()
    {
        RuleFor(x => x.AccessKey).NotEmpty();
        RuleFor(x => x.SecretKey).NotEmpty();
        RuleFor(x => x.SourceEmail).NotEmpty().EmailAddress();
    }
}
