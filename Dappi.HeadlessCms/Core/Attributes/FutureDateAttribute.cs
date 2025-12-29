using System.ComponentModel.DataAnnotations;

namespace Dappi.HeadlessCms.Core.Attributes;

public class FutureDateAttribute : ValidationAttribute
{
    public FutureDateAttribute()
    {
        ErrorMessage = "Date must not be in the past.";
    }

    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            DateTime dateTime => dateTime.Date >= DateTime.Today,
            DateOnly dateOnly => dateOnly >= DateOnly.FromDateTime(DateTime.Today),
            _ => false
        };
    }
}