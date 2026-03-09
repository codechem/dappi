using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Validators;
using FluentValidation.TestHelper;

namespace Dappi.HeadlessCms.Tests.Validators;

internal static class TestUserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public class UserDtoValidatorTests
{
    private readonly UserDtoValidator _validator = new();

    [Theory]
    [MemberData(nameof(UserDtoValidatorTestData.InvalidNameTestCases), MemberType = typeof(UserDtoValidatorTestData))]
    public void Should_have_error_for_invalid_Name(UserDto model, string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(UserDtoValidatorTestData.InvalidEmailTestCases), MemberType = typeof(UserDtoValidatorTestData))]
    public void Should_have_error_for_invalid_Email(UserDto model, string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(UserDtoValidatorTestData.InvalidRolesTestCases), MemberType = typeof(UserDtoValidatorTestData))]
    public void Should_have_error_for_invalid_Roles(UserDto model, string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        Assert.Contains(result.Errors, x => x.ErrorMessage == expectedErrorMessage);
    }

    [Fact]
    public void Should_not_have_error_for_valid_UserDto()
    {
        var model = new UserDto
        {
            Name = "name.name",
            Email = "name@test.com",
            Roles = [TestUserRoles.Admin, TestUserRoles.User],
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UserRolesUpdateDtoValidatorTests
{
    private readonly UserRolesUpdateDtoValidator _validator = new();

    [Theory]
    [MemberData(nameof(UserRolesUpdateDtoValidatorTestData.InvalidRolesTestCases), MemberType = typeof(UserRolesUpdateDtoValidatorTestData))]
    public void Should_have_error_for_invalid_Roles(UserRolesUpdateDto model, string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Fact]
    public void Should_not_have_error_for_valid_Roles()
    {
        var model = new UserRolesUpdateDto
        {
            Roles = [TestUserRoles.User],
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }
}

public static class UserDtoValidatorTestData
{
    public static IEnumerable<object[]> InvalidNameTestCases()
    {
        yield return new object[]
        {
            new UserDto { Name = string.Empty, Email = "name@test.com", Roles = [TestUserRoles.User] },
            "Username is required."
        };

        yield return new object[]
        {
            new UserDto { Name = null!, Email = "name@test.com", Roles = [TestUserRoles.User] },
            "Username is required."
        };

        yield return new object[]
        {
            new UserDto { Name = new string('a', 257), Email = "name@test.com", Roles = [TestUserRoles.User] },
            "Username must not exceed 256 characters."
        };
    }

    public static IEnumerable<object[]> InvalidEmailTestCases()
    {
        yield return new object[]
        {
            new UserDto { Name = "Name", Email = string.Empty, Roles = [TestUserRoles.User] },
            "Email is required."
        };

        yield return new object[]
        {
            new UserDto { Name = "Name", Email = null!, Roles = [TestUserRoles.User] },
            "Email is required."
        };

        yield return new object[]
        {
            new UserDto { Name = "Name", Email = "invalid-email", Roles = [TestUserRoles.User] },
            "A valid email address is required."
        };
    }

    public static IEnumerable<object[]> InvalidRolesTestCases()
    {
        yield return new object[]
        {
            new UserDto { Name = "Name", Email = "name@test.com", Roles = ["SuperAdmin"] },
            "Role is invalid."
        };
    }
}

public static class UserRoleUpdateDtoValidatorTestData
{
    public static IEnumerable<object[]> InvalidRoleTestCases()
    {
        yield return new object[]
        {
            new UserRoleUpdateDto { Role = string.Empty },
            "Role is required."
        };

        yield return new object[]
        {
            new UserRoleUpdateDto { Role = "SuperAdmin" },
            "Role is invalid."
        };
    }

    public static IEnumerable<object[]> ValidRoleTestCases()
    {
        yield return new object[] { new UserRoleUpdateDto { Role = TestUserRoles.Admin } };
    }
}

public static class UserRolesUpdateDtoValidatorTestData
{
    public static IEnumerable<object[]> InvalidRolesTestCases()
    {
        yield return new object[]
        {
            new UserRolesUpdateDto { Roles = null! },
            "Roles are required."
        };

        yield return new object[]
        {
            new UserRolesUpdateDto { Roles = [] },
            "Role cannot be empty."
        };
    }
}
