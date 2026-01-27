using System.Linq.Expressions;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Validators;
using FluentValidation.TestHelper;

namespace Dappi.HeadlessCms.Tests.Validators;

public class FieldRequestValidatorTests
{
    private readonly FieldRequestValidator _validator = new();

    [Theory]
    [MemberData(nameof(FieldRequestValidatorTestData.InvalidMinTestCases), MemberType = typeof(FieldRequestValidatorTestData))]
    public void Should_have_error_for_invalid_Min_value(FieldRequest model, Expression<Func<FieldRequest, object?>> propertyExpression, string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(FieldRequestValidatorTestData.ValidMinTestCases), MemberType = typeof(FieldRequestValidatorTestData))]
    public void Should_not_have_error_for_valid_Min_value(FieldRequest model, Expression<Func<FieldRequest, object?>> propertyExpression)
    {
        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(propertyExpression);
    }

    [Theory]
    [MemberData(nameof(FieldRequestValidatorTestData.InvalidMaxTestCases), MemberType = typeof(FieldRequestValidatorTestData))]
    public void Should_have_error_for_invalid_Max_value(FieldRequest model, Expression<Func<FieldRequest, object?>> propertyExpression, string expectedErrorMessage)
    {
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(propertyExpression)
            .WithErrorMessage(expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(FieldRequestValidatorTestData.ValidMaxTestCases), MemberType = typeof(FieldRequestValidatorTestData))]
    public void Should_not_have_error_for_valid_Max_value(
        FieldRequest model,
        Expression<Func<FieldRequest, object?>> propertyExpression)
    {
        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(propertyExpression);
    }

    [Theory]
    [MemberData(nameof(FieldRequestValidatorTestData.MinMaxComparisonTestCases), MemberType = typeof(FieldRequestValidatorTestData))]
    public void Should_validate_Min_Max_comparison(FieldRequest model, bool shouldHaveError, string? expectedErrorMessage)
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

public class FieldRequestValidatorTestData
{
    public static IEnumerable<object[]> InvalidMinTestCases()
    {
        Expression<Func<FieldRequest, object?>> minExpression = x => x.Min;
        const string errorMessage = "Min value is invalid for the field type.";

        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Min = -1 },
            minExpression,
            errorMessage
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Min = 5.5 },
            minExpression,
            errorMessage
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = double.NaN },
            minExpression,
            errorMessage
        };
    }

    public static IEnumerable<object[]> ValidMinTestCases()
    {
        Expression<Func<FieldRequest, object?>> minExpression = x => x.Min;
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Min = 0 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Min = 5 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Min = 100 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = 0 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = 5 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = -5 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = 100.5 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = -100.5 },
            minExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Min = null },
            minExpression
        };
    }

    public static IEnumerable<object[]> InvalidMaxTestCases()
    {
        Expression<Func<FieldRequest, object?>> maxExpression = x => x.Max;
        const string errorMessage = "Max value is invalid for the field type.";
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Max = -1 },
            maxExpression,
            errorMessage
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Max = 5.5 },
            maxExpression,
            errorMessage
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Max = double.NaN },
            maxExpression,
            errorMessage
        };
    }

    public static IEnumerable<object[]> ValidMaxTestCases()
    {
        Expression<Func<FieldRequest, object?>> maxExpression = x => x.Max;
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Max = 0 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Max = 5 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Max = 100 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Max = 0 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Max = 5 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Max = -5 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Max = 100.5 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Max = -100.5 },
            maxExpression
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "string", Max = null },
            maxExpression
        };
    }

    public static IEnumerable<object[]> MinMaxComparisonTestCases()
    {
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = 10, Max = 5 },
            true,
            "Min value cannot be greater than max value."
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = 5, Max = 5 },
            false,
            null!
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = 5, Max = 10 },
            false,
            null!
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = 5, Max = null },
            false,
            null!
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = null, Max = 10 },
            false,
            null!
        };
        
        yield return new object[]
        {
            new FieldRequest { FieldName = "TestField", FieldType = "int", Min = null, Max = null },
            false,
            null!
        };
    }
}

