using Dappi.HeadlessCms.Models;
using FluentValidation;

namespace Dappi.HeadlessCms.Validators;

public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(256)
            .WithMessage("Username must not exceed 256 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        RuleFor(x => x.Roles)
            .NotNull()
            .WithMessage("Roles are required.")
            .Must(roles => roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == roles.Count)
            .WithMessage("Roles must be unique.");

        RuleForEach(x => x.Roles)
            .NotEmpty()
            .WithMessage("Role cannot be empty.")
            .Must(role => Constants.UserRoles.All.Contains(role))
            .WithMessage("Role is invalid.");
    }
}

public class UserRoleUpdateDtoValidator : AbstractValidator<UserRoleUpdateDto>
{
    public UserRoleUpdateDtoValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role is required.")
            .Must(role => Constants.UserRoles.All.Contains(role))
            .WithMessage("Role is invalid.");
    }
}

public class UserRolesUpdateDtoValidator : AbstractValidator<UserRolesUpdateDto>
{
    public UserRolesUpdateDtoValidator()
    {
        RuleFor(x => x.Roles)
            .NotNull()
            .WithMessage("Roles are required.")
            .NotEmpty()
            .WithMessage("Role cannot be empty.");
    }
}