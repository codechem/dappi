using System.Linq.Expressions;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Validators;
using FluentValidation.TestHelper;

namespace Dappi.HeadlessCms.Tests.Validators;

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(LoginDtoValidatorTestData.InvalidUsernameTestCases),
        MemberType = typeof(LoginDtoValidatorTestData)
    )]
    public void Should_have_error_for_invalid_Username(
        LoginDto model,
        Expression<Func<LoginDto, object?>> propertyExpression,
        string expectedErrorMessage
    )
    {
        var result = _validator.TestValidate(model);

        result
            .ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(
        nameof(LoginDtoValidatorTestData.InvalidPasswordTestCases),
        MemberType = typeof(LoginDtoValidatorTestData)
    )]
    public void Should_have_error_for_invalid_Password(
        LoginDto model,
        Expression<Func<LoginDto, object?>> propertyExpression,
        string expectedErrorMessage
    )
    {
        var result = _validator.TestValidate(model);

        result
            .ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Fact]
    public void Should_not_have_error_for_valid_LoginDto()
    {
        var model = new LoginDto { Username = "admin", Password = "Dappi@123" };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(RegisterDtoValidatorTestData.InvalidUsernameTestCases),
        MemberType = typeof(RegisterDtoValidatorTestData)
    )]
    public void Should_have_error_for_invalid_Username(
        RegisterDto model,
        Expression<Func<RegisterDto, object?>> propertyExpression,
        string expectedErrorMessage
    )
    {
        var result = _validator.TestValidate(model);

        result
            .ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(
        nameof(RegisterDtoValidatorTestData.InvalidEmailTestCases),
        MemberType = typeof(RegisterDtoValidatorTestData)
    )]
    public void Should_have_error_for_invalid_Email(
        RegisterDto model,
        Expression<Func<RegisterDto, object?>> propertyExpression,
        string expectedErrorMessage
    )
    {
        var result = _validator.TestValidate(model);

        result
            .ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(
        nameof(RegisterDtoValidatorTestData.InvalidPasswordTestCases),
        MemberType = typeof(RegisterDtoValidatorTestData)
    )]
    public void Should_have_error_for_invalid_Password(
        RegisterDto model,
        Expression<Func<RegisterDto, object?>> propertyExpression,
        string expectedErrorMessage
    )
    {
        var result = _validator.TestValidate(model);

        result
            .ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Fact]
    public void Should_not_have_error_for_valid_RegisterDto()
    {
        var model = new RegisterDto
        {
            Username = "new.user",
            Email = "new.user@test.com",
            Password = "Dappi@123",
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateRoleDtoValidatorTests
{
    private readonly CreateRoleDtoValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(CreateRoleDtoValidatorTestData.InvalidNameTestCases),
        MemberType = typeof(CreateRoleDtoValidatorTestData)
    )]
    public void Should_have_error_for_invalid_Name(
        CreateRoleDto model,
        Expression<Func<CreateRoleDto, object?>> propertyExpression,
        string expectedErrorMessage
    )
    {
        var result = _validator.TestValidate(model);

        result
            .ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Fact]
    public void Should_not_have_error_for_valid_Name()
    {
        var model = new CreateRoleDto { Name = "Reviewer" };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}

public static class LoginDtoValidatorTestData
{
    public static IEnumerable<object[]> InvalidUsernameTestCases()
    {
        Expression<Func<LoginDto, object?>> usernameExpression = x => x.Username;

        yield return new object[]
        {
            new LoginDto { Username = string.Empty, Password = "Dappi@123" },
            usernameExpression,
            "Username is required.",
        };

        yield return new object[]
        {
            new LoginDto { Username = null!, Password = "Dappi@123" },
            usernameExpression,
            "Username is required.",
        };

        yield return new object[]
        {
            new LoginDto { Username = new string('a', 257), Password = "Dappi@123" },
            usernameExpression,
            "Username must not exceed 256 characters.",
        };
    }

    public static IEnumerable<object[]> InvalidPasswordTestCases()
    {
        Expression<Func<LoginDto, object?>> passwordExpression = x => x.Password;

        yield return new object[]
        {
            new LoginDto { Username = "admin", Password = string.Empty },
            passwordExpression,
            "Password is required.",
        };

        yield return new object[]
        {
            new LoginDto { Username = "admin", Password = null! },
            passwordExpression,
            "Password is required.",
        };
    }
}

public static class RegisterDtoValidatorTestData
{
    public static IEnumerable<object[]> InvalidUsernameTestCases()
    {
        Expression<Func<RegisterDto, object?>> usernameExpression = x => x.Username;

        yield return new object[]
        {
            new RegisterDto
            {
                Username = string.Empty,
                Email = "new.user@test.com",
                Password = "Dappi@123",
            },
            usernameExpression,
            "Username is required.",
        };

        yield return new object[]
        {
            new RegisterDto
            {
                Username = null!,
                Email = "new.user@test.com",
                Password = "Dappi@123",
            },
            usernameExpression,
            "Username is required.",
        };

        yield return new object[]
        {
            new RegisterDto
            {
                Username = new string('a', 257),
                Email = "new.user@test.com",
                Password = "Dappi@123",
            },
            usernameExpression,
            "Username must not exceed 256 characters.",
        };
    }

    public static IEnumerable<object[]> InvalidEmailTestCases()
    {
        Expression<Func<RegisterDto, object?>> emailExpression = x => x.Email;

        yield return new object[]
        {
            new RegisterDto
            {
                Username = "new.user",
                Email = string.Empty,
                Password = "Dappi@123",
            },
            emailExpression,
            "Email is required.",
        };

        yield return new object[]
        {
            new RegisterDto
            {
                Username = "new.user",
                Email = null!,
                Password = "Dappi@123",
            },
            emailExpression,
            "Email is required.",
        };

        yield return new object[]
        {
            new RegisterDto
            {
                Username = "new.user",
                Email = "invalid-email",
                Password = "Dappi@123",
            },
            emailExpression,
            "A valid email address is required.",
        };
    }

    public static IEnumerable<object[]> InvalidPasswordTestCases()
    {
        Expression<Func<RegisterDto, object?>> passwordExpression = x => x.Password;

        yield return new object[]
        {
            new RegisterDto
            {
                Username = "new.user",
                Email = "new.user@test.com",
                Password = string.Empty,
            },
            passwordExpression,
            "Password is required.",
        };

        yield return new object[]
        {
            new RegisterDto
            {
                Username = "new.user",
                Email = "new.user@test.com",
                Password = null!,
            },
            passwordExpression,
            "Password is required.",
        };

        yield return new object[]
        {
            new RegisterDto
            {
                Username = "new.user",
                Email = "new.user@test.com",
                Password = "12345",
            },
            passwordExpression,
            "Password must be at least 6 characters.",
        };
    }
}

public static class CreateRoleDtoValidatorTestData
{
    public static IEnumerable<object[]> InvalidNameTestCases()
    {
        Expression<Func<CreateRoleDto, object?>> nameExpression = x => x.Name;

        yield return new object[]
        {
            new CreateRoleDto { Name = string.Empty },
            nameExpression,
            "Role name is required",
        };

        yield return new object[]
        {
            new CreateRoleDto { Name = null },
            nameExpression,
            "Role name is required",
        };

        yield return new object[]
        {
            new CreateRoleDto { Name = "   " },
            nameExpression,
            "Role name is required",
        };
    }
}
