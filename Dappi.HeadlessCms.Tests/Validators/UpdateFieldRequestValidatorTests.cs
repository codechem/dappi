using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Validators;
using FluentValidation.TestHelper;

namespace Dappi.HeadlessCms.Tests.Validators;

public class UpdateFieldRequestValidatorTests
{
    private readonly UpdateFieldRequestValidator _validator = new();

    [Fact]
    public void Should_have_error_when_Min_is_negative_for_string_field()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Min = -1
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Min)
            .WithErrorMessage("Min value is invalid for the field type.");
    }

    [Fact]
    public void Should_have_error_when_Min_has_decimal_value_for_string_field()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Min = 5.5
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Min)
            .WithErrorMessage("Min value is invalid for the field type.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(100)]
    public void Should_not_have_error_when_Min_is_valid_for_string_field(double minValue)
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Min = minValue
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Min);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-5)]
    [InlineData(100.5)]
    [InlineData(-100.5)]
    public void Should_not_have_error_when_Min_is_valid_for_numeric_field(double minValue)
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = minValue
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Min);
    }

    [Fact]
    public void Should_have_error_when_Min_is_NaN_for_numeric_field()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = double.NaN
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Min)
            .WithErrorMessage("Min value is invalid for the field type.");
    }

    [Fact]
    public void Should_not_have_error_when_Min_is_null()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Min = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Min);
    }

    [Fact]
    public void Should_have_error_when_Max_is_negative_for_string_field()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Max = -1
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Max)
            .WithErrorMessage("Max value is invalid for the field type.");
    }

    [Fact]
    public void Should_have_error_when_Max_has_decimal_value_for_string_field()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Max = 5.5
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Max)
            .WithErrorMessage("Max value is invalid for the field type.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(100)]
    public void Should_not_have_error_when_Max_is_valid_for_string_field(double maxValue)
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Max = maxValue
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Max);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-5)]
    [InlineData(100.5)]
    [InlineData(-100.5)]
    public void Should_not_have_error_when_Max_is_valid_for_numeric_field(double maxValue)
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Max = maxValue
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Max);
    }

    [Fact]
    public void Should_have_error_when_Max_is_NaN_for_numeric_field()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Max = double.NaN
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Max)
            .WithErrorMessage("Max value is invalid for the field type.");
    }

    [Fact]
    public void Should_not_have_error_when_Max_is_null()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Max = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Max);
    }

    [Fact]
    public void Should_have_error_when_Min_is_greater_than_Max()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = 10,
            Max = 5
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Min value cannot be greater than max value.");
    }

    [Fact]
    public void Should_not_have_error_when_Min_equals_Max()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = 5,
            Max = 5
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_not_have_error_when_Min_is_less_than_Max()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = 5,
            Max = 10
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_not_have_error_when_only_Min_is_specified()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = 5,
            Max = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_not_have_error_when_only_Max_is_specified()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = null,
            Max = 10
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_not_have_error_when_both_Min_and_Max_are_null()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "int",
            Min = null,
            Max = null
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Should_have_errors_when_Min_and_Max_are_invalid_for_string_field()
    {
        var model = new UpdateFieldRequest
        {
            OldFieldName = "OldField",
            NewFieldName = "NewField",
            FieldType = "string",
            Min = -1,
            Max = 5.5
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Min);
        result.ShouldHaveValidationErrorFor(x => x.Max);
    }
}
