using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CargoFlow.Helpers;

public static class EnumCache<T> where T : struct, Enum
{
    private static readonly Dictionary<string, T> Mapping = Enum.GetValues<T>().ToDictionary(
        e => JsonNamingPolicy.SnakeCaseLower.ConvertName(e.ToString()),
        e => e,
        StringComparer.OrdinalIgnoreCase
    );

    public static T? ToEnum(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Mapping.TryGetValue(value, out var result) ? result : null;
    }

    public static T ToEnumRequired(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => ToEnum(value) ?? throw new ArgumentException($"The value '{value}' is not valid for {typeof(T).Name}.", paramName);
}
