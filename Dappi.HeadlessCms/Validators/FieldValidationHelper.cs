namespace Dappi.HeadlessCms.Validators;

public static class FieldValidationHelper
{
    public static readonly string[] TextTypes = { "string" };
    public static readonly string[] NumericTypes = { "int", "double", "float" };

    public static bool ValidateMinValue(string fieldType, double? min)
    {
        if (!min.HasValue)
            return true;

        if (TextTypes.Contains(fieldType))
        {
            return min.Value >= 0 && Math.Floor(min.Value) == min.Value;
        }

        if (NumericTypes.Contains(fieldType))
        {
            return !double.IsNaN(min.Value);
        }

        return true;
    }

    public static bool ValidateMaxValue(string fieldType, double? max)
    {
        if (!max.HasValue)
            return true;

        if (TextTypes.Contains(fieldType))
        {
            return max.Value >= 0 && Math.Floor(max.Value) == max.Value;
        }

        if (NumericTypes.Contains(fieldType))
        {
            return !double.IsNaN(max.Value);
        }

        return true;
    }

    public static bool ValidateMinMaxRelationship(double? min, double? max)
    {
        if (!min.HasValue || !max.HasValue)
            return true;

        return min.Value <= max.Value;
    }
}
