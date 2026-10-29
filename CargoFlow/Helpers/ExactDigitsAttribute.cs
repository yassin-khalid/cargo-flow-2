using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Helpers;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class ExactDigitsAttribute : RegularExpressionAttribute
{
    public ExactDigitsAttribute(int length) : base($@"^\d{{{length}}}$")
    {
        ErrorMessage = $"Must be exactly {length} digits";
    }
}
