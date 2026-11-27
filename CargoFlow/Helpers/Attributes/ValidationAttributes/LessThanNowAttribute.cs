using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Helpers.Attributes.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class LessThanNowAttribute : ValidationAttribute
{
    public LessThanNowAttribute()
    {
        ErrorMessage ??= "The date and time must be in the past";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            if (dateTimeOffset < DateTimeOffset.UtcNow)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(
                FormatErrorMessage(validationContext.DisplayName),
                validationContext.MemberName is not null
                    ? new[] { validationContext.MemberName }
                    : null
            );
        }
        return new ValidationResult("Invalid DateTimeOffset value");
    }
}
