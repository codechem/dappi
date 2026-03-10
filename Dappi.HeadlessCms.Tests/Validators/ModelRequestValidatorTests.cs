using System.Linq.Expressions;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Validators;
using FluentValidation.TestHelper;

namespace Dappi.HeadlessCms.Tests.Validators;

public class ModelRequestValidatorTests
{
    private readonly ModelRequestValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(ModelRequestValidatorTestData.InvalidModelNameTestCases),
        MemberType = typeof(ModelRequestValidatorTestData)
    )]
    public void Should_have_error_for_invalid_ModelName(
        ModelRequest model,
        Expression<Func<ModelRequest, object?>> propertyExpression,
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
        nameof(ModelRequestValidatorTestData.ValidModelNameTestCases),
        MemberType = typeof(ModelRequestValidatorTestData)
    )]
    public void Should_not_have_error_for_valid_ModelName(
        ModelRequest model,
        Expression<Func<ModelRequest, object?>> propertyExpression
    )
    {
        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(propertyExpression);
    }
}

public class ModelRequestValidatorTestData
{
    public static IEnumerable<object[]> InvalidModelNameTestCases()
    {
        Expression<Func<ModelRequest, object?>> modelNameExpression = x => x.ModelName;

        yield return new object[]
        {
            new ModelRequest { ModelName = string.Empty },
            modelNameExpression,
            "Model name must be provided.",
        };

        yield return new object[]
        {
            new ModelRequest { ModelName = null! },
            modelNameExpression,
            "Model name must be provided.",
        };

        yield return new object[]
        {
            new ModelRequest { ModelName = new string('a', 51) },
            modelNameExpression,
            "Model name must not exceed 50 characters.",
        };

        yield return new object[]
        {
            new ModelRequest { ModelName = "123Model" },
            modelNameExpression,
            "Model name is invalid.",
        };

        yield return new object[]
        {
            new ModelRequest { ModelName = "Model Name" },
            modelNameExpression,
            "Model name is invalid.",
        };
    }

    public static IEnumerable<object[]> ValidModelNameTestCases()
    {
        Expression<Func<ModelRequest, object?>> modelNameExpression = x => x.ModelName;

        yield return new object[]
        {
            new ModelRequest { ModelName = "Product" },
            modelNameExpression,
        };

        yield return new object[]
        {
            new ModelRequest { ModelName = "CustomerAddress" },
            modelNameExpression,
        };

        yield return new object[]
        {
            new ModelRequest { ModelName = "Entity2" },
            modelNameExpression,
        };
    }
}
