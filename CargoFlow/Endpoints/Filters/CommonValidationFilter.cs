using System.Text.Json;

namespace CargoFlow.Endpoints.Filters;

public enum ValidationType
{
    Required,
    Optional
}

public static class CommonValidationFilter
{

    public static void ValidateIntFilter(IQueryCollection query, string key, Dictionary<string, string[]> errors, ValidationType validationType = ValidationType.Optional)
    {
        if (!query.TryGetValue(key, out var keyString))
        {
            if (validationType == ValidationType.Required)
            {
                errors[key] = [$"{key} parameter is required please provide it"];
            }
            return;
        }
        if (!int.TryParse(keyString, out var keyValue) || keyValue < 1)
        {
            errors[key] = [$"{key} must be a valid positve integer"];
        }
    }

    public static void ValidateEnumFilter<T>(IQueryCollection query, string key, Dictionary<string, string[]> errors, ValidationType validationType = ValidationType.Optional) where T : struct, Enum
    {
        if (!query.TryGetValue(key, out var keyString) || string.IsNullOrWhiteSpace(keyString))
        {
            if (validationType == ValidationType.Required)
            {
                errors[key] = [$"{key} parameter is required please provide it"];
            }
            return;
        }
        var values = Enum.GetNames<T>().Select(JsonNamingPolicy.SnakeCaseLower.ConvertName).ToList();
        if (!values.Contains(keyString!))
        {
            errors[key] = [$"{key} must be one of '{string.Join(", ", values)}' values"];
        }
    }

    public static void ValidateBoolFilter(IQueryCollection query, string key, Dictionary<string, string[]> errors, ValidationType validationType = ValidationType.Optional)
    {
        if (!query.TryGetValue(key, out var keyString) || string.IsNullOrWhiteSpace(keyString))
        {
            if (validationType == ValidationType.Required)
            {
                errors[key] = [$"{key} parameter is required please provide it"];
            }
            return;
        }
        if (!bool.TryParse(keyString, out _))
        {
            errors[key] = [$"{key} must be a boolean value"];
        }
    }
}
