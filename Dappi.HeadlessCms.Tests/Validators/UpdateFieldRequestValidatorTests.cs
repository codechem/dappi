using System.Linq.Expressions;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Validators;
using FluentValidation.TestHelper;

namespace Dappi.HeadlessCms.Tests.Validators;

public class UpdateFieldRequestValidatorTests
{
    private readonly UpdateFieldRequestValidator _validator = new();

    [Theory]
    [MemberData(nameof(UpdateFieldValidationTestData.InvalidMinTestCases), MemberType = typeof(UpdateFieldValidationTestData))]
    public void Should_have_error_for_invalid_Min_value(
        UpdateFieldRequest model,
        Expression<Func<UpdateFieldRequest, object?>> propertyExpression,
        string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(UpdateFieldValidationTestData.ValidMinTestCases), MemberType = typeof(UpdateFieldValidationTestData))]
    public void Should_not_have_error_for_valid_Min_value(
        UpdateFieldRequest model,
        Expression<Func<UpdateFieldRequest, object?>> propertyExpression)
    {
        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(propertyExpression);
    }

    [Theory]
    [MemberData(nameof(UpdateFieldValidationTestData.InvalidMaxTestCases), MemberType = typeof(UpdateFieldValidationTestData))]
    public void Should_have_error_for_invalid_Max_value(
        UpdateFieldRequest model,
        Expression<Func<UpdateFieldRequest, object?>> propertyExpression,
        string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(UpdateFieldValidationTestData.ValidMaxTestCases), MemberType = typeof(UpdateFieldValidationTestData))]
    public void Should_not_have_error_for_valid_Max_value(
        UpdateFieldRequest model,
        Expression<Func<UpdateFieldRequest, object?>> propertyExpression)
    {
        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(propertyExpression);
    }

    [Theory]
    [MemberData(nameof(UpdateFieldValidationTestData.MinMaxComparisonTestCases), MemberType = typeof(UpdateFieldValidationTestData))]
    public void Should_validate_Min_Max_comparison(
        UpdateFieldRequest model,
        bool shouldHaveError,
        string? expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        if (shouldHaveError)
        {
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage(expectedErrorMessage!);
        }
        else
        {
            result.ShouldNotHaveValidationErrorFor(x => x);
        }
    }
}

public class UpdateFieldValidationTestData
{
    public static IEnumerable<object[]> InvalidMinTestCases()
    {
        Expression<Func<UpdateFieldRequest, object?>> minExpression = x => x.Min;
        const string errorMessage = "Min value is invalid for the field type.";

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Min = -1 },
            minExpression,
            errorMessage
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Min = 5.5 },
            minExpression,
            errorMessage
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = double.NaN },
            minExpression,
            errorMessage
        };
    }

    public static IEnumerable<object[]> ValidMinTestCases()
    {
        Expression<Func<UpdateFieldRequest, object?>> minExpression = x => x.Min;

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Min = 0 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Min = 5 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Min = 100 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = 0 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = 5 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = -5 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = 100.5 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = -100.5 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "double", Min = 5.5 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "float", Min = 5.5 },
            minExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Min = null },
            minExpression
        };
    }

    public static IEnumerable<object[]> InvalidMaxTestCases()
    {
        Expression<Func<UpdateFieldRequest, object?>> maxExpression = x => x.Max;
        const string errorMessage = "Max value is invalid for the field type.";

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Max = -1 },
            maxExpression,
            errorMessage
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Max = 5.5 },
            maxExpression,
            errorMessage
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Max = double.NaN },
            maxExpression,
            errorMessage
        };
    }

    public static IEnumerable<object[]> ValidMaxTestCases()
    {
        Expression<Func<UpdateFieldRequest, object?>> maxExpression = x => x.Max;

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Max = 0 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Max = 5 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Max = 100 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Max = 0 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Max = 5 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Max = -5 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Max = 100.5 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Max = -100.5 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "double", Max = 5.5 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "float", Max = 5.5 },
            maxExpression
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "string", Max = null },
            maxExpression
        };
    }

    public static IEnumerable<object[]> MinMaxComparisonTestCases()
    {
        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = 10, Max = 5 },
            true,
            "Min value cannot be greater than max value."
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = 5, Max = 5 },
            false,
            null!
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = 5, Max = 10 },
            false,
            null!
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = 5, Max = null },
            false,
            null!
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = null, Max = 10 },
            false,
            null!
        };

        yield return new object[]
        {
            new UpdateFieldRequest { OldFieldName = "OldField", NewFieldName = "NewField", FieldType = "int", Min = null, Max = null },
            false,
            null!
        };
    }
}
