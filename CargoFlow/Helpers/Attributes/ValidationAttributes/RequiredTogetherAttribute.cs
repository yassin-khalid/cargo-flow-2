using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Helpers.Attributes.ValidationAttributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class RequiredTogetherAttribute(string firstProperty, string secondProperty)
    : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var firstProp = value.GetType().GetProperty(firstProperty);
        if (firstProp is null)
        {
            return new ValidationResult($"Unknown property '{firstProperty}'");
        }
        var secondProp = value.GetType().GetProperty(secondProperty);
        if (secondProp is null)
        {
            return new ValidationResult($"Unknown property '{secondProperty}'");
        }

        var firstHasValue = firstProp.GetValue(value) is not null;
        var secondHasValue = secondProp.GetValue(value) is not null;

        if (firstHasValue == secondHasValue)
        {
            return ValidationResult.Success;
        }

        var message =
            ErrorMessage
            ?? $"{firstProperty} and {secondProperty} must both be provided together or be omitted.";
        return new ValidationResult(message, [firstProperty, secondProperty]);
    }
}
